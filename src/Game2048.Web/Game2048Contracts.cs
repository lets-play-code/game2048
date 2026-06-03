using Game2048StateModel = Game2048.Game.Game2048State;

namespace Game2048.Web;

public record MoveRequest(string Direction);
public record SaveLeaderboardRecordRequest(string PlayerName);
public record ControlledGameIdRequest(string Id);
public record GeneratedTileValueRequest(string Value);
public record SeedExistingGameRequest(string BoardJson, int Score, bool Win, bool Lose, bool ScoreRecorded, bool LeakedShouldAddTile);
public record ErrorResponse(string Error);
public record GameCreatedResponse(string Id, Game2048StateModel State);
