namespace FrontDTOs.StatusCodes{

public enum ClientError
{
	ParsingFailure = 401,
	ValidationError = 402,
	UserRegistrationError = 403,
	NotFound = 404,
	AuthenticationMissing = 405,
	AuthenticationExpired = 407,
	Unauthorized = 406,
	DuplicateEmail = 408
} }