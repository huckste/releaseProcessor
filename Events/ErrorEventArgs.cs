namespace ReleaseProcessor.Events;

using ErrorOr;

public record ErrorEventArgs(List<Error> Errors);
