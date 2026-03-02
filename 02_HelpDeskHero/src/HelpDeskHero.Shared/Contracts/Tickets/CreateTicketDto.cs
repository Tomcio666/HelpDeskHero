namespace HelpDeskHero.Shared.Contracts.Tickets;

public sealed class CreateTicketDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
}