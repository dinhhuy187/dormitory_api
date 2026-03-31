using Microsoft.EntityFrameworkCore;
using RoomService.API.Domain.Entities;
using RoomService.API.Domain.Enum;

namespace RoomService.API.Infrastructure.Database
{
    public static class SeedData
    {
        public static async Task SeedAsync(RoomDbContext context)
        {
            // Nếu database đã có dữ liệu tòa nhà thì bỏ qua, không seed lại
            if (await context.Buildings.AnyAsync())
            {
                return;
            }

            // ==========================================
            // 1. SEED ROOM TYPES
            // ==========================================
            var roomTypes = new List<RoomType>
            {
                new() { 
                    Name = "Phòng 8 sinh viên (Tiêu chuẩn)", Capacity = 8, BasePrice = 230000, 
                    Amenities = new List<string> { "Giường tầng", "Quạt trần", "Tủ đồ cá nhân", "Nhà vệ sinh khép kín" } 
                },
                new() { 
                    Name = "Phòng 6 sinh viên (Tiêu chuẩn)", Capacity = 6, BasePrice = 310000, 
                    Amenities = new List<string> { "Giường tầng", "Quạt trần", "Tủ đồ cá nhân", "Nhà vệ sinh khép kín" } 
                },
                new() { 
                    Name = "Phòng 6 sinh viên (Dịch vụ - Có máy lạnh)", Capacity = 6, BasePrice = 550000, 
                    Amenities = new List<string> { "Giường tầng", "Tủ đồ cá nhân", "Nhà vệ sinh khép kín", "Máy lạnh", "Rèm cửa" } 
                },
                new() { 
                    Name = "Phòng 4 sinh viên (Dịch vụ - Cơ bản)", Capacity = 4, BasePrice = 950000, 
                    Amenities = new List<string> { "Giường tầng", "Nhà vệ sinh khép kín", "Tủ đồ cá nhân" } 
                },
                new() { 
                    Name = "Phòng 4 sinh viên (Dịch vụ - Full tiện ích)", Capacity = 4, BasePrice = 1370000, 
                    Amenities = new List<string> { "Máy lạnh", "Rèm cửa", "Tủ lạnh", "Máy giặt", "Máy nước nóng", "Kệ dép" } 
                },
                new() { 
                    Name = "Phòng 2 sinh viên (Dịch vụ - VIP)", Capacity = 2, BasePrice = 3140000, 
                    Amenities = new List<string> { "Máy lạnh", "Rèm cửa", "Tủ lạnh", "Máy giặt", "Máy nước nóng", "Nệm", "Tủ", "Bàn", "Ghế" } 
                }
            };

            context.RoomTypes.AddRange(roomTypes);
            await context.SaveChangesAsync();

            // ==========================================
            // 2. SEED BUILDINGS
            // ==========================================
            var buildings = new List<Building>
            {
                // CỤM BA (12 tầng - Đa số là Nữ và một phần Nam)
                new() { ZoneName = "Cụm BA", Code = "BA1", Name = "Tòa nhà BA1", GenderRestriction = GenderRestriction.FemaleOnly, TotalFloors = 12 },
                new() { ZoneName = "Cụm BA", Code = "BA2", Name = "Tòa nhà BA2", GenderRestriction = GenderRestriction.FemaleOnly, TotalFloors = 12 },
                new() { ZoneName = "Cụm BA", Code = "BA3", Name = "Tòa nhà BA3", GenderRestriction = GenderRestriction.MaleOnly, TotalFloors = 12 },
                new() { ZoneName = "Cụm BA", Code = "BA4", Name = "Tòa nhà BA4", GenderRestriction = GenderRestriction.MaleOnly, TotalFloors = 12 },
                new() { ZoneName = "Cụm BA", Code = "BA5", Name = "Tòa nhà BA5 (Dịch vụ)", GenderRestriction = GenderRestriction.FemaleOnly, TotalFloors = 12 },

                // CỤM BB (16 tầng - Thường là các tòa cao nhất, phân bổ Nam/Nữ)
                new() { ZoneName = "Cụm BB", Code = "BB1", Name = "Tòa nhà BB1", GenderRestriction = GenderRestriction.MaleOnly, TotalFloors = 16 },
                new() { ZoneName = "Cụm BB", Code = "BB2", Name = "Tòa nhà BB2", GenderRestriction = GenderRestriction.FemaleOnly, TotalFloors = 16 },
                new() { ZoneName = "Cụm BB", Code = "BB3", Name = "Tòa nhà BB3 (Dịch vụ)", GenderRestriction = GenderRestriction.MaleOnly, TotalFloors = 16 },

                // CỤM BC (12 tầng - Nằm sâu bên trong, phân bổ Nam/Nữ)
                new() { ZoneName = "Cụm BC", Code = "BC1", Name = "Tòa nhà BC1", GenderRestriction = GenderRestriction.FemaleOnly, TotalFloors = 12 },
                new() { ZoneName = "Cụm BC", Code = "BC2", Name = "Tòa nhà BC2", GenderRestriction = GenderRestriction.FemaleOnly, TotalFloors = 12 },
                new() { ZoneName = "Cụm BC", Code = "BC3", Name = "Tòa nhà BC3", GenderRestriction = GenderRestriction.MaleOnly, TotalFloors = 12 },
                new() { ZoneName = "Cụm BC", Code = "BC4", Name = "Tòa nhà BC4", GenderRestriction = GenderRestriction.MaleOnly, TotalFloors = 12 },
                new() { ZoneName = "Cụm BC", Code = "BC5", Name = "Tòa nhà BC5", GenderRestriction = GenderRestriction.FemaleOnly, TotalFloors = 12 },
                new() { ZoneName = "Cụm BC", Code = "BC6", Name = "Tòa nhà BC6 (Dịch vụ)", GenderRestriction = GenderRestriction.MaleOnly, TotalFloors = 12 },

                // CỤM BD (12 tầng - Gần khu vực sân thể thao, căn tin lớn)
                new() { ZoneName = "Cụm BD", Code = "BD1", Name = "Tòa nhà BD1", GenderRestriction = GenderRestriction.MaleOnly, TotalFloors = 12 },
                new() { ZoneName = "Cụm BD", Code = "BD2", Name = "Tòa nhà BD2", GenderRestriction = GenderRestriction.FemaleOnly, TotalFloors = 12 },
                new() { ZoneName = "Cụm BD", Code = "BD3", Name = "Tòa nhà BD3", GenderRestriction = GenderRestriction.MaleOnly, TotalFloors = 12 },
                new() { ZoneName = "Cụm BD", Code = "BD4", Name = "Tòa nhà BD4", GenderRestriction = GenderRestriction.FemaleOnly, TotalFloors = 12 },
                new() { ZoneName = "Cụm BD", Code = "BD5", Name = "Tòa nhà BD5 (Dịch vụ)", GenderRestriction = GenderRestriction.MaleOnly, TotalFloors = 12 },
                new() { ZoneName = "Cụm BD", Code = "BD6", Name = "Tòa nhà BD6 (Dịch vụ)", GenderRestriction = GenderRestriction.FemaleOnly, TotalFloors = 12 },

                // CỤM BE (Tên thực tế thường gọi là E1, E2... - Tòa mới, thiết kế hiện đại)
                new() { ZoneName = "Cụm BE", Code = "E1", Name = "Tòa nhà E1", GenderRestriction = GenderRestriction.FemaleOnly, TotalFloors = 12 },
                new() { ZoneName = "Cụm BE", Code = "E2", Name = "Tòa nhà E2", GenderRestriction = GenderRestriction.MaleOnly, TotalFloors = 12 },
                new() { ZoneName = "Cụm BE", Code = "E3", Name = "Tòa nhà E3 (Dịch vụ)", GenderRestriction = GenderRestriction.FemaleOnly, TotalFloors = 12 },
                new() { ZoneName = "Cụm BE", Code = "E4", Name = "Tòa nhà E4 (Dịch vụ)", GenderRestriction = GenderRestriction.MaleOnly, TotalFloors = 12 },
                new() { ZoneName = "Cụm BE", Code = "E5", Name = "Tòa nhà E5 (Dịch vụ VIP)", GenderRestriction = GenderRestriction.FemaleOnly, TotalFloors = 12 }
            };

            context.Buildings.AddRange(buildings);
            await context.SaveChangesAsync();

            // ==========================================
            // 3. SEED ROOMS 
            // ==========================================
            var roomsToInsert = new List<Room>();
            var random = new Random();

            var type8 = roomTypes.First(rt => rt.Capacity == 8).Id;
            var type6Std = roomTypes.First(rt => rt.Capacity == 6 && rt.BasePrice == 310000).Id;
            var type6Ac = roomTypes.First(rt => rt.Capacity == 6 && rt.BasePrice == 550000).Id;
            var type4Std = roomTypes.First(rt => rt.Capacity == 4 && rt.BasePrice == 950000).Id;
            var type4Vip = roomTypes.First(rt => rt.Capacity == 4 && rt.BasePrice == 1370000).Id;
            var type2Vip = roomTypes.First(rt => rt.Capacity == 2).Id;

            foreach (var building in buildings)
            {
                bool isServiceBuilding = building.Name.Contains("Dịch vụ");

                for (int floor = 1; floor <= building.TotalFloors; floor++)
                {
                    // Khu B thường có từ 20 đến 24 phòng mỗi tầng
                    for (int roomIndex = 1; roomIndex <= 20; roomIndex++) 
                    {
                        string roomNumber = $"{floor}{(roomIndex < 10 ? "0" : "")}{roomIndex}";
                        Guid assignedTypeId;

                        if (isServiceBuilding)
                        {
                            // Tòa dịch vụ: Đa số là phòng máy lạnh 6, 4 và 2 người
                            int rand = random.Next(1, 100);
                            if (rand <= 50) assignedTypeId = type6Ac;
                            else if (rand <= 80) assignedTypeId = type4Std;
                            else if (rand <= 95) assignedTypeId = type4Vip;
                            else assignedTypeId = type2Vip; // Tỷ lệ phòng 2 người cực hiếm
                        }
                        else
                        {
                            // Tòa tiêu chuẩn: 100% là phòng 8 người hoặc 6 người không máy lạnh
                            assignedTypeId = random.Next(1, 100) <= 85 ? type8 : type6Std;
                        }

                        int capacity = roomTypes.First(rt => rt.Id == assignedTypeId).Capacity;
                        
                        int currentOccupied = 0;
                        RoomStatus currentStatus;

                        if (random.Next(1, 100) <= 3)
                        {
                            currentStatus = RoomStatus.Maintenance;
                            currentOccupied = 0; // Ép số lượng người ở về 0
                        }
                        else
                        {
                            // 97% số phòng còn lại hoạt động bình thường
                            currentOccupied = random.Next(0, capacity + 1);
                            currentStatus = currentOccupied == capacity ? RoomStatus.Full : RoomStatus.Available;
                        }

                        roomsToInsert.Add(new Room
                        {
                            BuildingId = building.Id,
                            RoomTypeId = assignedTypeId,
                            RoomNumber = roomNumber,
                            Floor = floor,
                            RoomStatus = currentStatus,
                            OccupiedCount = currentOccupied
                        });
                    }
                }
            }

            context.Rooms.AddRange(roomsToInsert);
            await context.SaveChangesAsync();
        }
    }
}