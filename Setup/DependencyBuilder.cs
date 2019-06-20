namespace AlwaysDecrypted.Setup
{
	using AlwaysDecrypted.Data;
	using Autofac;

	public class DependencyBuilder
	{
		public static IContainer Build()
		{
			var builder = new ContainerBuilder();
			builder.RegisterType<ColumnEncryptionRepository>().AsImplementedInterfaces();
			builder.RegisterType<ConnectionStringBuilder>().AsImplementedInterfaces();
			builder.RegisterType<ConnectionFactory>().AsImplementedInterfaces();
			builder.RegisterType<ColumnEncryptionQueryFactory>().AsImplementedInterfaces();
			return builder.Build();
		}
	}
}
