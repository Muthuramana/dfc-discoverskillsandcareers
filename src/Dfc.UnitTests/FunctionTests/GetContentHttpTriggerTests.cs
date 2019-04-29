﻿using Dfc.DiscoverSkillsAndCareers.ContentFunctionApp;
using Dfc.DiscoverSkillsAndCareers.Repositories;
using Dfc.UnitTests.Fakes;
using DFC.Common.Standard.Logging;
using DFC.HTTP.Standard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dfc.DiscoverSkillsAndCareers.ContentFunctionApp.ContentApi;
using Xunit;

namespace Dfc.UnitTests.FunctionTests
{
    public class GetContentHttpTriggerTests : IDisposable
    {
        public GetContentHttpTriggerTests()
        {
            _request = new DefaultHttpRequest(new DefaultHttpContext());
            _log = Substitute.For<ILogger>();
            _httpRequestHelper = Substitute.For<IHttpRequestHelper>();
            _httpResponseMessageHelper = Substitute.For<IHttpResponseMessageHelper>();
            _contentRepository = Substitute.For<IContentRepository>();
        }

        public void Dispose()
        {
            _httpRequestHelper = null;
            _httpResponseMessageHelper = null;
            _contentRepository = null;
        }

        private HttpRequest _request;
        private ILogger _log;
        private IHttpRequestHelper _httpRequestHelper;
        private IHttpResponseMessageHelper _httpResponseMessageHelper;
        private IContentRepository _contentRepository;

        private async Task<HttpResponseMessage> RunFunction(string contentType)
        {
            return await GetContentHttpTrigger.Run(
                _request,
                contentType,
                _log,
                _httpRequestHelper,
                _httpResponseMessageHelper,
                _contentRepository
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task GetContentHttpTrigger_WithInvalidContent_ShouldReturnStatusCodeNoContent()
        {
            _httpResponseMessageHelper = new HttpResponseMessageHelper();

            var result = await RunFunction("invalid-page-content-name");

            Assert.IsType<HttpResponseMessage>(result);
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        }

        [Fact]
        public async Task GetContentHttpTrigger_WithValidContent_ShouldReturnStatusCodeNoContent()
        {
            _httpResponseMessageHelper = new HttpResponseMessageHelper();
            _contentRepository = new FakeContentRepository();

            var result = await RunFunction("startpage");

            Assert.IsType<HttpResponseMessage>(result);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }
    }
}
