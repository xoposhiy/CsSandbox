using System;
using System.Net;
using System.Net.Http;

namespace CsSandboxApi
{
	public class CsSandboxClientException : Exception
	{
		public readonly HttpStatusCode Status;
		public readonly string Reason;

		private CsSandboxClientException(HttpStatusCode status, string reason) : base(String.Format("{0}: {1}", status, reason))
		{
			Status = status;
			Reason = reason;
		}

		private static CsSandboxClientException Create(HttpStatusCode status, String reason)
		{
			if (status == HttpStatusCode.Unauthorized)
				return new Unauthorized(reason);
			if (status == HttpStatusCode.Forbidden)
				return new Forbidden(reason);
			if (status == HttpStatusCode.NotFound)
				return new SubmissionNotFound(reason);
			if (status == HttpStatusCode.BadRequest)
				return new BadRequest(reason);
			return new CsSandboxClientException(status, reason);
		}

		public class Unauthorized : CsSandboxClientException
		{
			public Unauthorized(string reason) : base(HttpStatusCode.Unauthorized, reason)
			{
			}
		}

		public class Forbidden : CsSandboxClientException
		{
			public Forbidden(string reason) : base(HttpStatusCode.Forbidden, reason)
			{
			}
		}

		public class SubmissionNotFound : CsSandboxClientException
		{
			public SubmissionNotFound(string reason) : base(HttpStatusCode.NotFound, reason)
			{
			}
		}

		private class BadRequest : CsSandboxClientException
		{

			public BadRequest(string reason) : base(HttpStatusCode.BadRequest, reason)
			{
			}
		}

		public static CsSandboxClientException Create(HttpResponseMessage response)
		{
			return Create(response.StatusCode, response.ReasonPhrase);
		}
	}
}