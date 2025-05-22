namespace JobTrackerApp.Models
{
    public enum SectionType
    {
        Permit = 1,
        Demolition = 2,
        RoughPlumbing = 3,
        RoughElectrical = 4,
        Framing = 5,
        Insulation = 6,
        Sheetrock = 7,
        PaintPrep = 8,
        FinishInstall = 9,
        FinishPaintFlooring = 10,
        KitchenBathFixtures = 11,
        Miscellaneous = 12
    }

    public enum SectionStatus
    {
        NotStarted = 1,
        InProgress = 2,
        Completed = 3,
        Denied = 4,
        OnHold = 5,
        NeedsInspection = 6,
        PassedInspection = 7,
        FailedInspection = 8
    }

    public static class SectionTypeExtensions
    {
        public static string GetDisplayName(this SectionType sectionType)
        {
            return sectionType switch
            {
                SectionType.Permit => "Permit",
                SectionType.Demolition => "Demolition",
                SectionType.RoughPlumbing => "Rough Plumbing",
                SectionType.RoughElectrical => "Rough Electrical",
                SectionType.Framing => "Framing",
                SectionType.Insulation => "Insulation",
                SectionType.Sheetrock => "Sheetrock",
                SectionType.PaintPrep => "Paint Prep",
                SectionType.FinishInstall => "Finish Install",
                SectionType.FinishPaintFlooring => "Finish Paint/Flooring",
                SectionType.KitchenBathFixtures => "Kitchen & Bath Fixtures",
                SectionType.Miscellaneous => "Miscellaneous",
                _ => "Unknown"
            };
        }

        public static string GetStatusName(this SectionStatus status)
        {
            return status switch
            {
                SectionStatus.NotStarted => "Not Started",
                SectionStatus.InProgress => "In Progress",
                SectionStatus.Completed => "Completed",
                SectionStatus.Denied => "Denied",
                SectionStatus.OnHold => "On Hold",
                SectionStatus.NeedsInspection => "Needs Inspection",
                SectionStatus.PassedInspection => "Passed Inspection",
                SectionStatus.FailedInspection => "Failed Inspection",
                _ => "Unknown"
            };
        }
    }
}
