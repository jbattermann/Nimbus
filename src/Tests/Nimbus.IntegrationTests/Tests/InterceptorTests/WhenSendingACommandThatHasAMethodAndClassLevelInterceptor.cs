﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Nimbus.Configuration;
using Nimbus.Infrastructure.DependencyResolution;
using Nimbus.IntegrationTests.Tests.InterceptorTests.Handlers;
using Nimbus.IntegrationTests.Tests.InterceptorTests.Interceptors;
using Nimbus.IntegrationTests.Tests.InterceptorTests.MessageContracts;
using Nimbus.Tests.Common;
using NUnit.Framework;
using Shouldly;

namespace Nimbus.IntegrationTests.Tests.InterceptorTests
{
    [TestFixture]
    [Timeout(TimeoutSeconds*1000)]
    public class WhenSendingACommandThatHasAMethodAndClassLevelInterceptor : SpecificationForAsync<IBus>
    {
        private const int _expectedTotalCallCount = 11; // 5 interceptors * 2 + 1 handler
        public const int TimeoutSeconds = 15;

        protected override async Task<IBus> Given()
        {
            MethodCallCounter.Clear();

            var testFixtureType = GetType();
            var typeProvider = new TestHarnessTypeProvider(new[] {testFixtureType.Assembly}, new[] {testFixtureType.Namespace});
            var logger = TestHarnessLoggerFactory.Create();

            var bus = new BusBuilder().Configure()
                                      .WithNames("MyTestSuite", Environment.MachineName)
                                      .WithConnectionString(CommonResources.ServiceBusConnectionString)
                                      .WithTypesFrom(typeProvider)
                                      .WithDependencyResolver(new DependencyResolver(typeProvider))
                                      .WithDefaultTimeout(TimeSpan.FromSeconds(10))
                                      .WithMaxDeliveryAttempts(1)
                                      .WithGlobalInboundInterceptorTypes(typeof (SomeGlobalInterceptor))
                                      .WithLogger(logger)
                                      .WithDebugOptions(
                                          dc =>
                                          dc.RemoveAllExistingNamespaceElementsOnStartup(
                                              "I understand this will delete EVERYTHING in my namespace. I promise to only use this for test suites."))
                                      .Build();
            await bus.Start();

            return bus;
        }

        protected override async Task When()
        {
            await Subject.Send(new FooCommand());
            await TimeSpan.FromSeconds(TimeoutSeconds).WaitUntil(() => MethodCallCounter.TotalReceivedCalls >= _expectedTotalCallCount);

            MethodCallCounter.Stop();
            MethodCallCounter.Dump();
        }

        [Test]
        public async Task TheCommandBrokerShouldReceiveThatCommand()
        {
            MethodCallCounter.ReceivedCallsWithAnyArg<MultipleCommandHandler>(h => h.Handle((FooCommand) null)).Count().ShouldBe(1);
        }

        [Test]
        public async Task TheMethodLevelExecutingInterceptorShouldHaveBeenInvoked()
        {
            MethodCallCounter.ReceivedCallsWithAnyArg<SomeMethodLevelInterceptorForFoo>(i => i.OnCommandHandlerExecuting<FooCommand>(null, null)).Count().ShouldBe(1);
        }

        [Test]
        public async Task TheMethodLevelSuccessInterceptorShouldHaveBeenInvoked()
        {
            MethodCallCounter.ReceivedCallsWithAnyArg<SomeMethodLevelInterceptorForFoo>(i => i.OnCommandHandlerSuccess<FooCommand>(null, null)).Count().ShouldBe(1);
        }

        [Test]
        public async Task TheBaseMethodLevelExecutingInterceptorShouldHaveBeenInvoked()
        {
            MethodCallCounter.ReceivedCallsWithAnyArg<SomeBaseMethodLevelInterceptorForFoo>(i => i.OnCommandHandlerExecuting<FooCommand>(null, null)).Count().ShouldBe(1);
        }

        [Test]
        public async Task TheBaseMethodLevelSuccessInterceptorShouldHaveBeenInvoked()
        {
            MethodCallCounter.ReceivedCallsWithAnyArg<SomeBaseMethodLevelInterceptorForFoo>(i => i.OnCommandHandlerSuccess<FooCommand>(null, null)).Count().ShouldBe(1);
        }

        [Test]
        public async Task TheBaseClassLevelExecutingInterceptorShouldHaveBeenInvoked()
        {
            MethodCallCounter.ReceivedCallsWithAnyArg<SomeBaseClassLevelInterceptor>(i => i.OnCommandHandlerExecuting<FooCommand>(null, null)).Count().ShouldBe(1);
        }

        [Test]
        public async Task TheBaseClassLevelSuccessInterceptorShouldHaveBeenInvoked()
        {
            MethodCallCounter.ReceivedCallsWithAnyArg<SomeBaseClassLevelInterceptor>(i => i.OnCommandHandlerSuccess<FooCommand>(null, null)).Count().ShouldBe(1);
        }

        [Test]
        public async Task TheClassLevelExecutingInterceptorShouldHaveBeenInvoked()
        {
            MethodCallCounter.ReceivedCallsWithAnyArg<SomeClassLevelInterceptor>(i => i.OnCommandHandlerExecuting<FooCommand>(null, null)).Count().ShouldBe(1);
        }

        [Test]
        public async Task TheClassLevelSuccessInterceptorShouldHaveBeenInvoked()
        {
            MethodCallCounter.ReceivedCallsWithAnyArg<SomeClassLevelInterceptor>(i => i.OnCommandHandlerSuccess<FooCommand>(null, null)).Count().ShouldBe(1);
        }

        [Test]
        public async Task TheGlobalExecutingInterceptorShouldHaveBeenInvoked()
        {
            MethodCallCounter.ReceivedCallsWithAnyArg<SomeGlobalInterceptor>(i => i.OnCommandHandlerExecuting<FooCommand>(null, null)).Count().ShouldBe(1);
        }

        [Test]
        public async Task TheGlobalSuccessInterceptorShouldHaveBeenInvoked()
        {
            MethodCallCounter.ReceivedCallsWithAnyArg<SomeGlobalInterceptor>(i => i.OnCommandHandlerSuccess<FooCommand>(null, null)).Count().ShouldBe(1);
        }

        [Test]
        public async Task TheCorrectNumberOfInterceptorsShouldHaveBeenInvoked()
        {
            MethodCallCounter.TotalReceivedCalls.ShouldBe(_expectedTotalCallCount);
        }
    }
}