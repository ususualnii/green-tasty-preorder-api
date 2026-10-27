using Microsoft.AspNetCore.Mvc;

namespace GreenTasty.Preorder.Api.Controllers;

[ApiController]
[Route("guest/groups")]
public class GuestGroupsController(Store store) : ControllerBase
{
    private User? Caller => HttpContext.Items["user"] as User;
    private bool IsMyGroup(GroupReservation group) => Caller?.Id == group.OwnerId;

    /// <summary>Level 1 — everyone who joined the group pre-order: dishes, per-person totals, grand total.</summary>
    [HttpGet("{groupReservationId}/sessions")]
    [OwnerOnly]
    public ActionResult<GroupSnapshot> GetSessions(string groupReservationId)
    {
        var group = store.Groups.GetValueOrDefault(groupReservationId);
        if (group is null) return NotFound(new ErrorResponse($"Group reservation '{groupReservationId}' not found"));
        if (!IsMyGroup(group)) return StatusCode(403, new ErrorResponse("Only the owner of this group can view it"));

        return store.Snapshot(group);
    }

    /// <summary>Level 3 — remove a participant's session from the group and return the updated snapshot.</summary>
    [HttpDelete("{groupReservationId}/sessions/{guestSessionId}")]
    [OwnerOnly]
    public ActionResult<GroupSnapshot> DeleteSession(string groupReservationId, string guestSessionId)
    {
        var group = store.Groups.GetValueOrDefault(groupReservationId);
        if (group is null) return NotFound(new ErrorResponse($"Group reservation '{groupReservationId}' not found"));
        if (!IsMyGroup(group)) return StatusCode(403, new ErrorResponse("Only the owner of this group can change it"));

        var session = store.Groups.Values.SelectMany(g => g.Sessions).FirstOrDefault(s => s.Id == guestSessionId);
        if (session is null) return NotFound(new ErrorResponse($"Guest session '{guestSessionId}' not found"));

        if (!group.Sessions.Contains(session))
            return BadRequest(new ErrorResponse($"Guest session '{guestSessionId}' does not belong to group '{groupReservationId}'"));

        group.Sessions.Remove(session);
        return store.Snapshot(group);
    }
}
