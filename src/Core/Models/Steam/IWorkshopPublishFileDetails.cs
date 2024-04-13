namespace ModManager.Models.Steam;

public interface IWorkshopPublishFileDetails
{
	long PublishedFileId { get; set; }
	long TimeCreated { get; set; }
	long TimeUpdated { get; set; }

	List<WorkshopTag>? Tags { get; set; }
}
