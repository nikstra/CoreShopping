using System;
using System.Collections.Generic;
using System.Text;

namespace nikstra.CoreShopping.Service.Models
{
    public class ShopUserLogin
    {
        public virtual string LoginProvider { get; set; }
        public virtual string ProviderKey { get; set; }
        public virtual string ProviderDisplayName { get; set; }
        public virtual string UserId { get; set; }
        //public string UserId { get; set; }
        //public virtual ICollection<ShopUser> User { get; set; }
    }
}
/*
 TODO: Fix the primary key {'LoginProvider' : string, 'ProviderKey' : string} configuration, see below:

Test Name:	Dummy
Test FullName:	nikstra.CoreShopping.Service.Tests.Class1.Dummy
Test Source:	C:\Users\Niklas\Source\Repos\nikstra.CoreShopping\tests\nikstra.CoreShopping.Service.Tests\Class1.cs : line 13
Test Outcome:	Failed
Test Duration:	0:00:00,331

Result StackTrace:	
at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNoShadowKeys(IModel model)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.CreateModel(DbContext context, IConventionSetBuilder conventionSetBuilder, IModelValidator validator)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.<>c__DisplayClass5_0.<GetModel>b__0(Object k)
   at System.Collections.Concurrent.ConcurrentDictionary`2.GetOrAdd(TKey key, Func`2 valueFactory)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, IConventionSetBuilder conventionSetBuilder, IModelValidator validator)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel()
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
   at Microsoft.EntityFrameworkCore.Infrastructure.EntityFrameworkServicesBuilder.<>c.<TryAddCoreServices>b__7_1(IServiceProvider p)
   at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitFactory(FactoryCallSite factoryCallSite, ServiceProvider provider)
   at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(IServiceCallSite callSite, TArgument argument)
   at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitScoped(ScopedCallSite scopedCallSite, ServiceProvider provider)
   at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(IServiceCallSite callSite, TArgument argument)
   at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitConstructor(ConstructorCallSite constructorCallSite, ServiceProvider provider)
   at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(IServiceCallSite callSite, TArgument argument)
   at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitScoped(ScopedCallSite scopedCallSite, ServiceProvider provider)
   at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(IServiceCallSite callSite, TArgument argument)
   at Microsoft.Extensions.DependencyInjection.ServiceProvider.<>c__DisplayClass22_0.<RealizeService>b__0(ServiceProvider provider)
   at Microsoft.Extensions.DependencyInjection.ServiceProvider.GetService(Type serviceType)
   at Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService(IServiceProvider provider, Type serviceType)
   at Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService[T](IServiceProvider provider)
   at Microsoft.EntityFrameworkCore.DbContext.get_DbContextDependencies()
   at Microsoft.EntityFrameworkCore.DbContext.get_InternalServiceProvider()
   at Microsoft.EntityFrameworkCore.DbContext.get_DbContextDependencies()
   at Microsoft.EntityFrameworkCore.DbContext.EntryWithoutDetectChanges[TEntity](TEntity entity)
   at Microsoft.EntityFrameworkCore.DbContext.SetEntityState[TEntity](TEntity entity, EntityState entityState)
   at Microsoft.EntityFrameworkCore.DbContext.Add[TEntity](TEntity entity)
   at Microsoft.EntityFrameworkCore.Internal.InternalDbSet`1.Add(TEntity entity)
   at nikstra.CoreShopping.Service.Tests.Class1.<Dummy>d__0.MoveNext() in C:\Users\Niklas\Source\Repos\nikstra.CoreShopping\tests\nikstra.CoreShopping.Service.Tests\Class1.cs:line 21
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
   at NUnit.Framework.Internal.AsyncInvocationRegion.AsyncTaskInvocationRegion.WaitForPendingOperationsToComplete(Object invocationResult) in C:\src\nunit\nunit\src\NUnitFramework\framework\Internal\AsyncInvocationRegion.cs:line 113
   at NUnit.Framework.Internal.Commands.TestMethodCommand.RunAsyncTestMethod(TestExecutionContext context) in C:\src\nunit\nunit\src\NUnitFramework\framework\Internal\Commands\TestMethodCommand.cs:line 96
Result Message:	System.InvalidOperationException : The relationship from 'ShopUser' to 'ShopUserLogin' with foreign key properties {'Id' : string} cannot target the primary key {'LoginProvider' : string, 'ProviderKey' : string} because it is not compatible. Configure a principal key or a set of compatible foreign key properties for this relationship.


 */
