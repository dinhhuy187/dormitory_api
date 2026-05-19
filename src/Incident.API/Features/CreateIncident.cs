using FluentValidation;
using Incident.API.Domain.Entities;
using Incident.API.Infrastructure.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using System.Security.Claims;

namespace Incident.API.Features;

public static class CreateIncident
{
    public record Request(
        Guid RoomId,
        Guid CategoryId,
        string Description,
        IFormFileCollection? Images = null  // ← Fix: thêm = null và đổi sang IFormFileCollection
    );

    public record Response(
        Guid Id,
        Guid RoomId,
        Guid CategoryId,
        string Description,
        string Status,
        DateTime CreatedAt
    );

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.RoomId)
                .NotEmpty().WithMessage("RoomId không được để trống.");

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("CategoryId không được để trống.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Mô tả không được để trống.")
                .MinimumLength(10).WithMessage("Mô tả phải có ít nhất 10 ký tự.")
                .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự.");

            RuleFor(x => x.Images)
                .Must(images => images == null || images.Count <= 5)
                .WithMessage("Tối đa 5 ảnh.");

            RuleForEach(x => x.Images).ChildRules(image =>
            {
                image.RuleFor(f => f.Length)
                    .LessThanOrEqualTo(5 * 1024 * 1024)
                    .WithMessage("Dung lượng ảnh quá lớn (Tối đa 5MB).");

                image.RuleFor(f => f.FileName)
                    .Must(HaveValidExtension)
                    .WithMessage("Định dạng file không hỗ trợ.");
            }).When(x => x.Images != null && x.Images.Count > 0);  // ← Fix: thêm .Count > 0
        }

        private bool HaveValidExtension(string fileName)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(fileName).ToLower();
            return allowedExtensions.Contains(extension);
        }
    }

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/incidents", async (
                [FromForm] Request request,
                HttpContext httpContext,
                [FromServices] Handler handler,
                [FromServices] IValidator<Request> validator,
                CancellationToken ct) =>
            {
                var validation = await validator.ValidateAsync(request, ct);
                if (!validation.IsValid)
                    return Results.ValidationProblem(validation.ToDictionary());

                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirstValue("sub")
                    ?? throw new UnauthorizedAccessException();

                var result = await handler.ExecuteAsync(userId, request, ct);
                return Results.Created(
                    $"/api/incidents/{result.Id}",
                    new ApiResponse<Response>(result));
            })
            .WithTags("Incidents")
            .WithName("CreateIncident")
            .RequireAuthorization(policy => policy.RequireRole("Student"))
            .DisableAntiforgery()
            .Produces<Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem();
        }
    }

    public class Handler(IncidentDbContext dbContext, IMediaService mediaService)
    {
        public async Task<Response> ExecuteAsync(string userId, Request req, CancellationToken ct)
        {
            var categoryExists = await dbContext.IncidentCategories
                .AnyAsync(c => c.Id == req.CategoryId, ct);

            if (!categoryExists)
                throw new ApiException("Danh mục sự cố không tồn tại.", StatusCodes.Status404NotFound);

            var uploadedUrls = new List<string>();
            if (req.Images != null && req.Images.Count > 0)
            {
                foreach (var file in req.Images)
                {
                    var url = await mediaService.UploadImageAsync(file, "incident_images");
                    uploadedUrls.Add(url);
                }
            }

            var incident = new Domain.Entities.Incident
            {
                RoomId = req.RoomId,
                ReporterId = userId,
                CategoryId = req.CategoryId,
                Description = req.Description,
                ImageUrls = uploadedUrls,
            };

            dbContext.Incidents.Add(incident);
            await dbContext.SaveChangesAsync(ct);

            return new Response(
                incident.Id,
                incident.RoomId,
                incident.CategoryId,
                incident.Description,
                incident.Status.ToString(),
                incident.CreatedAt
            );
        }
    }
}