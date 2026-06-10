namespace AnnouncementBot.Domain.Enums
{
    public enum DeliveryErrorStatus
    {
        None = 0,
        BadRequest = 400,
        Unauthorized = 401,
        Forbidden = 403,
        NotFound = 404,
        TooManyRequests = 429,
        InternalServerError = 500,
        NetworkError = -1
    }
}
