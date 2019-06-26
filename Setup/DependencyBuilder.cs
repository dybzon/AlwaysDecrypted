namespace AlwaysDecrypted.Setup
{
	using AlwaysDecrypted.Data;
    using AlwaysDecrypted.Services;
    using Autofac;

	public static class DependencyBuilder
	{
		public static IContainer Container { get; private set; }

		public static IContainer Build()
		{
			var builder = new ContainerBuilder();
			builder.RegisterType<ColumnEncryptionRepository>().AsImplementedInterfaces();
			builder.RegisterType<ConnectionStringBuilder>().AsImplementedInterfaces();
			builder.RegisterType<ConnectionFactory>().AsImplementedInterfaces();
			builder.RegisterType<ColumnEncryptionQueryFactory>().AsImplementedInterfaces();
			builder.RegisterType<DataDecryptionService>().AsImplementedInterfaces();
			builder.RegisterType<DataTypeDeclarationBuilder>().AsImplementedInterfaces();
			builder.RegisterType<PrimaryKeyValidationService>().AsImplementedInterfaces();
			return Container = builder.Build();
		}
	}
}
