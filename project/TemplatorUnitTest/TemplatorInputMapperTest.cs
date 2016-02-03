using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetUtils;
using Templator;

namespace TemplatorUnitTest
{
    [TestClass]
    public class TemplatorInputMapperTest
    {
        class Employee
        {
            public string FirstName;
            public string LastName;
        }

        class Company
        {
            public string Name;
            public IList<Employee> Employees;
        }

        //Define a customized context to store used entities (Optional)
        class TestContext : TextHolderMappingContext
        {
            public Company Company;
            public Employee CurrentEmployee;
        }

        [TestMethod]
        public void MapInput()
        {
            const string tem =
                @"  {{Data(Field1)}}
                    {{Data(Employers)[Collection]}}
                        {{(FirstName)}}
                        {{Calculated(Wages)}},
                        {{Data(PaymentAmount)}},
                        {{Data(Months)}},
                        {{Data(Employees)[Collection]}}
                            {{User(Index)}},
                            {{User(Custom)}},
                            {{User(Wages)}},
                            {{(FirstName)}}
                            {{Data(Employment)[Collection]}}
                                {{(FirstName)}}
                            {{Data(Employment)[CollectionEnd]}}
                        {{Data(Employees)[CollectionEnd]}}
                    {{Data(Employers)[CollectionEnd]}}
                    {{Data(Field2)}}";
            var parser = new TemplatorParser(TemplatorConfig.DefaultInstance);
            parser.ParseText(tem, null);
            //Get the holder definitions
            var fields = parser.Holders;

            //initialize a config instance
            var config = new TemplatorInputMapper<TestContext>();
            //map for names, 
            config.AddNameResolver("Employers")
                .AsCollectionContext() //for collections, resolve as a collection of child contexts
                .ResolveAs(new List<TestContext> { new TestContext() { Company = new Company() {Name = "C1", Employees = new List<Employee>() {new Employee() {FirstName = "C1E1"}, new Employee() { FirstName = "C1E2" } } }} });
            config.AddNameResolver("Employees").AsCollectionContext()
                //resolve as a collection of contexts, set context's references at this point
                .ResolveAs((holder, context) =>{return context.Company?.Employees?.Select(e => new TestContext() {CurrentEmployee = e, Company = context.Company}).ToArray();});
            config.AddNameResolver("Employment").AsCollectionContext().ResolveAs((holder, context) => (new TestContext() {CurrentEmployee = context.CurrentEmployee}).Single());
            //Simple name resolver
            config.AddNameResolver("Field1").ResolveAs("(HolderValueField1)");
            //Resolve by category with inner logic
            config.AddCategoryResolver("Calculated").ResolveAs((holder, context) =>{var c = context.Root.Input["Calculated"];return c;});
            config.AddNameResolver("Wages").ResolveAs("WillNeedToShow");
            //Resolve by hierarchy
            config.AddHierarchyResolver("Employers.Employees").ResolveAs("Employers.Employees");
            //Customized matching
            config.AddCustomResolver().MatchWith((holder, context) => context.CurrentEmployee != null && holder.Name == "Custom").ResolveAs("CustomResoved");
            //Retrieve collection items' indexes
            config.AddNameResolver("Index").ResolveAs((holder, context) => context.CollectionIndex);
            //Resolve by name and match only in specific level
            config.AddNameResolver("FirstName").SpecifyHierarchies("Employers.Employees.Employment").ResolveAs((holder, context) => context.CurrentEmployee.FirstName);
            config.AddNameResolver("FirstName")
                //Retrieve context values
                .ResolveAs((holder, context) => context.Company.Name);
            config.AddCategoryResolver("User").ResolveAs("UserField");
            config.AddCategoryResolver("Data").ResolveAs("DataField");
            //Set a default resolver
            config.SetDefaultResolver((holder, context) => holder.Name);

            //it is recommended to create a new instance for processing from the config instance, rather than use the config instance to process
            //for better maintenance as well as thread safety
            var mapper = new TemplatorInputMapper<TestContext>(config)
            {
                Logger = new TemplatorLogger()
            }; ;
            //enable resolvers in priority order
            mapper.EnableNameResolvers();
            mapper.EnableCategoryResolvers();
            mapper.EnableHierachyResolvers();
            mapper.EnableCustomResolvers();
            var input = mapper.GenerateInput(fields, new TestContext() {Input = new Dictionary<string, object>() { { "Calculated", 1 } } });

            Assert.IsTrue(mapper.Logger.IsEmpty());
            parser.StartOver();

            var parsed = parser.ParseText(tem, input);

            Assert.AreEqual(@"  (HolderValueField1)
                    
                        C1
                        WillNeedToShow,
                        DataField,
                        DataField,
                        
                            0,
                            UserField,
                            WillNeedToShow,
                            C1
                            
                                C1E1
                            
                        
                            1,
                            UserField,
                            WillNeedToShow,
                            C1
                            
                                C1E2
                            
                        
                    
                    DataField", parsed);

        }
    }
}
