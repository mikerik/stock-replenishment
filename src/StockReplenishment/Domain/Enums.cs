using System.Text.Json.Serialization;

namespace StockReplenishment.Domain;

[JsonConverter(typeof(JsonStringEnumConverter<RequestStatus>))]
public enum RequestStatus { Draft, Submitted, Approved, Rejected, Fulfilled }

[JsonConverter(typeof(JsonStringEnumConverter<Priority>))]
public enum Priority { Low, Normal, Urgent }

[JsonConverter(typeof(JsonStringEnumConverter<StockCheckStatus>))]
public enum StockCheckStatus { NotStarted, Pending, Completed, Failed }