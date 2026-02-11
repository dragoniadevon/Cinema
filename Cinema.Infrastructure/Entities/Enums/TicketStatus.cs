public enum TicketStatus : short
{
    Reserved = 1,   // місце зайняте, але не оплачено
    Paid = 2,       // оплачено
    Cancelled = 3   // скасовано / повернуто
}
