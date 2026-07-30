using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;
using Zonkey.Extensions;
using Zonkey.ObjectModel.QueryTranslation;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit.QueryTranslation
{
    public class PartialEvaluatorTests
    {
        private static int GetSeven() => 7;
        private static readonly int StaticField = 42;

        private sealed class Holder
        {
            public int Id { get; set; }
            public Holder Inner { get; set; }
        }

        private static ConstantExpression RightConstant<T>(Expression<Func<T, bool>> expr)
        {
            var reduced = (BinaryExpression)PartialEvaluator.Reduce(expr.Body);
            return Assert.IsAssignableFrom<ConstantExpression>(reduced.Right);
        }

        [Fact]
        public void ClosureVariable_BecomesConstant()
        {
            int id = 5;
            Assert.Equal(5, RightConstant<Animal>(a => a.SpeciesId == id).Value);
        }

        [Fact]
        public void MethodCallResult_BecomesConstant()
        {
            Assert.Equal(7, RightConstant<Animal>(a => a.SpeciesId == GetSeven()).Value);
        }

        [Fact]
        public void Indexer_BecomesConstant()
        {
            var ids = new[] { 10, 20, 30 };
            Assert.Equal(20, RightConstant<Animal>(a => a.SpeciesId == ids[1]).Value);
        }

        [Fact]
        public void NestedPropertyChain_BecomesConstant()
        {
            var h = new Holder { Inner = new Holder { Id = 99 } };
            Assert.Equal(99, RightConstant<Animal>(a => a.SpeciesId == h.Inner.Id).Value);
        }

        [Fact]
        public void StaticField_BecomesConstant()
        {
            Assert.Equal(42, RightConstant<Animal>(a => a.SpeciesId == StaticField).Value);
        }

        [Fact]
        public void ArithmeticOnLocals_Folds()
        {
            int qty = 3;
            Assert.Equal(12, RightConstant<Animal>(a => a.SpeciesId == qty * 4).Value);
        }

        [Fact]
        public void DateTimeNowArithmetic_Folds()
        {
            DateTime before = DateTime.Now;
            Expression<Func<Animal, bool>> expr = a => a.DateOfBirth > DateTime.Now.AddDays(-7);
            var reduced = (BinaryExpression)PartialEvaluator.Reduce(expr.Body);
            DateTime after = DateTime.Now;

            var c = Assert.IsAssignableFrom<ConstantExpression>(reduced.Right);
            var when = Assert.IsType<DateTime>(c.Value);
            Assert.InRange(when, before.AddDays(-7), after.AddDays(-7));
        }

        [Fact]
        public void StaticProperty_ChainedIndexer_CapturedTernary_AllFold()
        {
            var dict = new Dictionary<string, int[]> { ["a"] = new[] { 5, 6 } };
            bool flag = true;
            Assert.Equal(6, RightConstant<Animal>(a => a.SpeciesId == dict["a"][1]).Value);
            Assert.Equal(9, RightConstant<Animal>(a => a.SpeciesId == (flag ? 9 : 1)).Value);
            var reduced = (BinaryExpression)PartialEvaluator.Reduce(((Expression<Func<Animal, bool>>)(a => a.DateOfBirth > DateTime.Today)).Body);
            Assert.IsAssignableFrom<ConstantExpression>(reduced.Right);
        }

        [Fact]
        public void ParameterSide_IsNotEvaluated()
        {
            Expression<Func<Animal, bool>> expr = a => a.SpeciesId == 1;
            var reduced = (BinaryExpression)PartialEvaluator.Reduce(expr.Body);
            Assert.IsAssignableFrom<MemberExpression>(reduced.Left);
        }

        [Fact]
        public void SqlInSubqueryLambdas_Survive()
        {
            Expression<Func<Animal, bool>> expr =
                a => a.ExhibitId.SqlIn((Exhibit e) => e.ExhibitId, e => e.IsOpen);
            var reduced = PartialEvaluator.Reduce(expr.Body);
            var call = Assert.IsAssignableFrom<MethodCallExpression>(reduced);
            Assert.Equal("SqlIn", call.Method.Name);
            Assert.IsAssignableFrom<LambdaExpression>(call.Arguments[1]);
            Assert.IsAssignableFrom<LambdaExpression>(call.Arguments[2]);
        }

        [Fact]
        public void NullClosureVariable_BecomesTypedNullConstant()
        {
            string name = null;
            var c = RightConstant<Animal>(a => a.Name == name);
            Assert.Null(c.Value);
            Assert.Equal(typeof(string), c.Type);
        }

        private static int Boom() => throw new InvalidOperationException("boom");

        [Fact]
        public void UserCodeException_UnwrapsTargetInvocationException()
        {
            Expression<Func<Animal, bool>> expr = a => a.SpeciesId == Boom();
            var ex = Assert.Throws<InvalidOperationException>(() => PartialEvaluator.Reduce(expr.Body));
            Assert.Equal("boom", ex.Message);
        }
    }
}
