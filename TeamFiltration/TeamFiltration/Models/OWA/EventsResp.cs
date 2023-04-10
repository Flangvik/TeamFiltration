using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.OWA
{


    public class EventsResp
    {
        public string odatacontext { get; set; }
        public List<EventsRespValue> value { get; set; }
        public string odatanextLink { get; set; }
    }

    public class EventsRespValue
    {
        public string odataid { get; set; }
        public string odataetag { get; set; }
        public string Id { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime LastModifiedDateTime { get; set; }
        public string ChangeKey { get; set; }
        public object[] Categories { get; set; }
        public object TransactionId { get; set; }
        public string OriginalStartTimeZone { get; set; }
        public string OriginalEndTimeZone { get; set; }
        public string iCalUId { get; set; }
        public int ReminderMinutesBeforeStart { get; set; }
        public bool IsReminderOn { get; set; }
        public bool HasAttachments { get; set; }
        public string Subject { get; set; }
        public string BodyPreview { get; set; }
        public string Importance { get; set; }
        public string Sensitivity { get; set; }
        public bool IsAllDay { get; set; }
        public bool IsCancelled { get; set; }
        public bool IsOrganizer { get; set; }
        public bool IsRoomRequested { get; set; }
        public string AutoRoomBookingStatus { get; set; }
        public bool ResponseRequested { get; set; }
        public object SeriesMasterId { get; set; }
        public string ShowAs { get; set; }
        public string Type { get; set; }
        public string WebLink { get; set; }
        public object OnlineMeetingUrl { get; set; }
        public bool IsOnlineMeeting { get; set; }
        public string OnlineMeetingProvider { get; set; }
        public bool AllowNewTimeProposals { get; set; }
        public object OccurrenceId { get; set; }
        public bool IsDraft { get; set; }
        public bool HideAttendees { get; set; }
        public object[] CalendarEventClassifications { get; set; }
        public bool MuteNotifications { get; set; }
        public Responsestatus ResponseStatus { get; set; }
        public EventsResp Body { get; set; }
        public Start Start { get; set; }
        public End End { get; set; }
        public Location Location { get; set; }
        public object[] Locations { get; set; }
        public Recurrence Recurrence { get; set; }
        public object AutoRoomBookingOptions { get; set; }
        public Attendee[] Attendees { get; set; }
        public Organizer Organizer { get; set; }
        public Onlinemeeting OnlineMeeting { get; set; }
        public string CalendarodataassociationLink { get; set; }
        public string CalendarodatanavigationLink { get; set; }
    }

    public class Responsestatus
    {
        public string Response { get; set; }
        public DateTime Time { get; set; }
    }

    public class EventsRespBody
    {
        public string ContentType { get; set; }
        public string Content { get; set; }
    }

    public class Start
    {
        public DateTime DateTime { get; set; }
        public string TimeZone { get; set; }
    }

    public class End
    {
        public DateTime DateTime { get; set; }
        public string TimeZone { get; set; }
    }

    public class Location
    {
        public string DisplayName { get; set; }
        public string LocationType { get; set; }
        public string UniqueIdType { get; set; }
        public Address Address { get; set; }
        public Coordinates Coordinates { get; set; }
    }

    public class Address
    {
        public string Type { get; set; }
    }

    public class Coordinates
    {
    }

    public class Recurrence
    {
        public Pattern Pattern { get; set; }
        public Range Range { get; set; }
    }

    public class Pattern
    {
        public string Type { get; set; }
        public int Interval { get; set; }
        public int Month { get; set; }
        public int DayOfMonth { get; set; }
        public string[] DaysOfWeek { get; set; }
        public string FirstDayOfWeek { get; set; }
        public string Index { get; set; }
    }

    public class Range
    {
        public string Type { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string RecurrenceTimeZone { get; set; }
        public int NumberOfOccurrences { get; set; }
    }

    public class Organizer
    {
        public Emailaddress EmailAddress { get; set; }
    }

    public class Emailaddress
    {
        public string Name { get; set; }
        public string Address { get; set; }
    }

    public class Onlinemeeting
    {
        public string JoinUrl { get; set; }
    }

    public class Attendee
    {
        public string Type { get; set; }
        public Status Status { get; set; }
        public Emailaddress1 EmailAddress { get; set; }
    }

    public class Status
    {
        public string Response { get; set; }
        public DateTime Time { get; set; }
    }

    public class Emailaddress1
    {
        public string Name { get; set; }
        public string Address { get; set; }
    }

  
}
