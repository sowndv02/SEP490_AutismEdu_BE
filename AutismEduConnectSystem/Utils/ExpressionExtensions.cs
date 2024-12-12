using System.Linq.Expressions;

namespace AutismEduConnectSystem.Utils
{
    public static class ExpressionExtensions
    {
        public static Expression<Func<T, bool>> AndAlso<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
        {
            // Ensure both expressions use the same parameter
            var parameter = Expression.Parameter(typeof(T), "param");

            // Replace parameters in both expressions with the unified parameter
            var left = new ReplaceExpressionVisitor(expr1.Parameters[0], parameter).Visit(expr1.Body);
            var right = new ReplaceExpressionVisitor(expr2.Parameters[0], parameter).Visit(expr2.Body);

            // Combine both expressions with AndAlso
            var combined = Expression.AndAlso(left, right);

            // Return a new expression lambda using the unified parameter
            return Expression.Lambda<Func<T, bool>>(combined, parameter);
        }
    }
    public class ReplaceExpressionVisitor : ExpressionVisitor
    {
        private readonly Expression _from;
        private readonly Expression _to;

        public ReplaceExpressionVisitor(Expression from, Expression to)
        {
            _from = from;
            _to = to;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            // Replace the parameter if it matches the old one
            return node == _from ? _to : base.VisitParameter(node);
        }
    }


}
