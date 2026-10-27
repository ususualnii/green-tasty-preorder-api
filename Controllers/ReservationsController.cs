using Microsoft.AspNetCore.Mvc;

namespace GreenTasty.Preorder.Api.Controllers;

[ApiController]
[Route("reservations")]
public class ReservationsController(Store store) : ControllerBase
{
    /// <summary>Level 2 — guestSessionId of the owner's own (owner) session for this reservation.</summary>
    [HttpGet("{reservationId}/guest-session-id")]
    [OwnerOnly]
    public ActionResult<GuestSessionIdResponse> GetGuestSessionId(string reservationId)
    {
        var reservation = store.Reservations.GetValueOrDefault(reservationId);
        if (reservation is null) return NotFound(new ErrorResponse($"Reservation '{reservationId}' not found"));

        var caller = HttpContext.Items["user"] as User;
        if (caller?.Id != reservation.OwnerId)
            return StatusCode(403, new ErrorResponse("Only the owner of this reservation can call this endpoint"));

        var ownerSession = store.Groups.Values.SelectMany(g => g.Sessions)
            .FirstOrDefault(s => s.ReservationId == reservationId && s.Type == ParticipantType.Owner);
        if (ownerSession is null)
            return NotFound(new ErrorResponse($"Owner session for reservation '{reservationId}' not found"));

        return new GuestSessionIdResponse(ownerSession.Id);
    }
}
