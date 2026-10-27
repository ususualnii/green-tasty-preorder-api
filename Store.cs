namespace GreenTasty.Preorder.Api;

/// <summary>In-memory data for the demo: users, dishes, reservations and groups with seed data.</summary>
public class Store
{
    public Dictionary<string, User> Users { get; } = [];
    public Dictionary<string, Dish> Dishes { get; } = [];
    public Dictionary<string, Reservation> Reservations { get; } = [];
    public Dictionary<string, GroupReservation> Groups { get; } = [];

    // ---------- reading ----------

    public int Total(GuestSession session) => session.Items.Sum(i => Dishes[i.DishId].Price * i.Quantity);

    public GroupSnapshot Snapshot(GroupReservation group) => new(
        group.Sessions.Select(s => new ParticipantDto(
            s.Id, s.Type.ToString(), s.Name,
            s.Items.Select(ToDto).ToList(),
            Total(s),
            s.Status == SessionStatus.Submitted)).ToList(),
        group.Sessions.Sum(Total));

    private ItemDto ToDto(SessionItem item)
    {
        var dish = Dishes[item.DishId];
        return new ItemDto(new DishDto(dish.Id, dish.Name, dish.Price), item.Quantity);
    }

    // ---------- seed ----------

    public static Store Seed()
    {
        var s = new Store();

        s.Dishes["dish1"] = new Dish { Id = "dish1", Name = "Caesar Salad", Price = 180 };
        s.Dishes["dish2"] = new Dish { Id = "dish2", Name = "Tom Yum", Price = 220 };
        s.Dishes["dish3"] = new Dish { Id = "dish3", Name = "Grilled Salmon", Price = 320 };

        s.Users["owner-1"] = new User { Id = "owner-1", Name = "Andrii", Role = Role.Owner };
        s.Users["guest-1"] = new User { Id = "guest-1", Name = "Olena", Role = Role.Guest };

        // owner reservation with an order + a reservation without an order (for the 400 "order not found" case)
        s.Reservations["reservation-123"] = new Reservation
        {
            Id = "reservation-123", OwnerId = "owner-1", GroupId = "group-1",
            Order = new Order { Id = "order-123" }
        };
        s.Reservations["reservation-888"] = new Reservation { Id = "reservation-888", OwnerId = "owner-1", GroupId = "group-1" };
        s.Reservations["reservation-999"] = new Reservation { Id = "reservation-999", OwnerId = "owner-1", GroupId = "group-2" };
        s.Reservations["reservation-g1"] = new Reservation { Id = "reservation-g1", OwnerId = "guest-1", GroupId = "group-1" };

        // group-1 — the main demo: 3 submitted sessions + 1 draft (draft is skipped on submit)
        s.Groups["group-1"] = new GroupReservation
        {
            Id = "group-1", OwnerId = "owner-1",
            Sessions =
            [
                new GuestSession
                {
                    Id = "owner-session-123", ReservationId = "reservation-123", Name = "Andrii",
                    Type = ParticipantType.Owner, Status = SessionStatus.Submitted,
                    Items = [new SessionItem { DishId = "dish1", Quantity = 2 }]
                },
                new GuestSession
                {
                    Id = "session-1", ReservationId = "reservation-g1", Name = "Olena",
                    Type = ParticipantType.Guest, Status = SessionStatus.Submitted,
                    Items = [new SessionItem { DishId = "dish1", Quantity = 1 }, new SessionItem { DishId = "dish2", Quantity = 1 }]
                },
                new GuestSession
                {
                    Id = "session-2", ReservationId = "reservation-g2", Name = "Petro",
                    Type = ParticipantType.Guest, Status = SessionStatus.Submitted,
                    Items = [new SessionItem { DishId = "dish2", Quantity = 1 }]
                },
                new GuestSession
                {
                    Id = "session-3", ReservationId = "reservation-g3", Name = "Iryna",
                    Type = ParticipantType.Guest, Status = SessionStatus.Draft,
                    Items = [new SessionItem { DishId = "dish3", Quantity = 1 }]
                }
            ]
        };

        // group-2 — nothing submitted yet (400 "no submitted sessions")
        s.Groups["group-2"] = new GroupReservation
        {
            Id = "group-2", OwnerId = "owner-1",
            Sessions =
            [
                new GuestSession
                {
                    Id = "owner-session-999", ReservationId = "reservation-999", Name = "Andrii",
                    Type = ParticipantType.Owner, Status = SessionStatus.Draft,
                    Items = [new SessionItem { DishId = "dish1", Quantity = 1 }]
                }
            ]
        };

        // group-3 — submitted session without dishes (400 "no dishes")
        s.Groups["group-3"] = new GroupReservation
        {
            Id = "group-3", OwnerId = "owner-1",
            Sessions =
            [
                new GuestSession
                {
                    Id = "session-4", ReservationId = "reservation-g1", Name = "Olena",
                    Type = ParticipantType.Guest, Status = SessionStatus.Submitted, Items = []
                }
            ]
        };

        return s;
    }
}
