namespace AnnouncementBot.Domain.Enums {
    public enum DeliveryErrorStatus
    {
        None = 0,
        BadRequest = 400,          // Неверный chat_id, пустой текст или ошибки разметки (Markdown/HTML)
        Unauthorized = 401,        // Невалидный или истекший API токен
        Forbidden = 403,           // Бот заблокирован пользователем или исключен из группы
        NotFound = 404,            // Метод или чат не существует
        TooManyRequests = 429,     // Превышен лимит (Flood limit). Нужно ждать retry_after
        InternalServerError = 500  // Ошибка на стороне серверов 
    }

}
