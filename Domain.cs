namespace GreenTasty.Preorder.Api;

public enum Role { Owner, Guest }
public enum ParticipantType { Owner, Guest }
public enum SessionStatus { Draft, Submitted }
public enum OrderStatus { Draft, SubmittedByCustomer }
public enum GroupStatus { Collecting, Submitted }

public class User
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public Role Role { get; set; }
}

public class Dish
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int Price { get; set; }
}

public class SessionItem
{
    public string DishId { get; set; } = "";
    public int Quantity { get; set; }
}

/// <summary>One participant's cart inside a group pre-order (owner or guest).</summary>
public class GuestSession
{
    public string Id { get; set; } = "";
    public string ReservationId { get; set; } = "";
    public string Name { get; set; } = "";
    public ParticipantType Type { get; set; }
    public SessionStatus Status { get; set; }
    public List<SessionItem> Items { get; set; } = [];
}

public class Order
{
    public string Id { get; set; } = "";
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public List<SessionItem> Items { get; set; } = [];
}

public class Reservation
{
    public string Id { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public string? GroupId { get; set; }
    public Order? Order { get; set; }
}

public class GroupReservation
{
    public string Id { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public GroupStatus Status { get; set; } = GroupStatus.Collecting;
    public List<GuestSession> Sessions { get; set; } = [];
}
