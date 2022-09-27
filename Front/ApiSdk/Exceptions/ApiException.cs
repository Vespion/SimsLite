using System;
using FrontDTOs;

namespace ApiSdk.Exceptions
{
	public class ApiException : Exception
	{
		/// <inheritdoc />
		public ApiException(ResponseWrapper responseWrapper)
		{
			ResponseWrapper = responseWrapper;
		}

		public ResponseWrapper ResponseWrapper { get; }
	}

	public class RegistrationException : ApiException
	{
		/// <inheritdoc />
		public RegistrationException(ResponseWrapper responseWrapper) : base(responseWrapper)
		{
		}
	}

	public class DuplicateEmailException : RegistrationException
	{
		/// <inheritdoc />
		public DuplicateEmailException(ResponseWrapper responseWrapper, string email) : base(responseWrapper)
		{
			Email = email;
		}

		public string Email { get; set; }
	}

	public class ResourceNotFoundException : ApiException
	{
		/// <inheritdoc />
		public ResourceNotFoundException(ResponseWrapper responseWrapper, string targetResource, string targetType) : base(responseWrapper)
		{
			TargetResource = targetResource;
			TargetType = targetType;
		}

		public string TargetResource { get; }

		public string TargetType { get; }
	}
}