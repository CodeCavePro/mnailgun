using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using Typesafe.Mailgun.Http;

namespace Typesafe.Mailgun
{
	internal abstract class MailgunCommand<T> where T : CommandResult
	{
		private readonly string path;
		private readonly string httpVerb;

		protected MailgunCommand(IMailgunAccountInfo accountInfo, string path, string httpVerb = "POST")
		{
			this.path = path;
			this.httpVerb = httpVerb;
			AccountInfo = accountInfo;
		}

		protected IMailgunAccountInfo AccountInfo { get; private set; }

		public T Invoke()
		{
			var request = new MailgunHttpRequest(AccountInfo, httpVerb, path);

			request.SetFormParts(CreateFormParts());

			var response = request.GetResponse();

			ThrowIfBadStatusCode(response);

			return TranslateResponse(response);
		}

		private static void ThrowIfBadStatusCode(MailgunHttpResponse response)
		{
            switch(response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                     throw new AuthenticationException();

                case HttpStatusCode internalError when internalError >= HttpStatusCode.InternalServerError:
                    var errorMessage = string.IsNullOrWhiteSpace(response?.Message)
                        ? "Internal Server Error"
                        : $"Internal Server Error: {response.Message}";
                    throw new Exception(errorMessage);

                case HttpStatusCode badRequest when badRequest >= HttpStatusCode.BadRequest && !(response?.Message?.StartsWith("Great job!") ?? false):
                    throw new InvalidOperationException(response.Message);
            }
		}

		protected internal virtual IEnumerable<FormPart> CreateFormParts()
		{
			return Enumerable.Empty<FormPart>();
		}

		public abstract T TranslateResponse(MailgunHttpResponse response);
	}
}