namespace AlwaysDecryptedTests.Settings
{
	using AlwaysDecrypted.Settings;
	using Xunit;
	using FakeItEasy;
	using FluentAssertions;
    using AlwaysDecrypted.Logging;
    using System;
    using AlwaysDecrypted.Models;
    using System.Collections.Generic;

    public class SettingsBuilderTests
	{
		[Theory]
		[ClassData(typeof(TestData))]
		public void SettingsBuilderShouldBuildExpectedSettings(string[] args, Settings expectedSettings)
		{
			var settings = new Settings();
			var settingsBuilder = new SettingsBuilder(A.Fake<ILogger>(), settings);
			settingsBuilder.BuildSettings(args);
			settings.Should().BeEquivalentTo(expectedSettings);
		}

		[Fact]
		public void SettingsBuilderShouldThrowOnInvalidInput()
		{
			var settingsBuilder = new SettingsBuilder(A.Fake<ILogger>(), A.Fake<ISettings>());
			Action build = () => settingsBuilder.BuildSettings(new[] { "foobar", "shizzle" });
			build.Should().Throw<IndexOutOfRangeException>();
		}

		private class TestData : TheoryData<string[], Settings>
		{
			public TestData()
			{
				this.Add(new[] { "-server=.", "-db=yomamma" }, new Settings { Server = ".", Database = "yomamma" });
				this.Add(new[] { "-server=localhost", "-database=yomamma", "-foo=bar", "-dinmor=john" }, new Settings { Server = "localhost", Database = "yomamma" });
				this.Add(new[] { "-tables=Foo.Bar, lol.kek, dbo.stufu  " }, new Settings { TablesToDecrypt = new List<Table> { new Table("Foo.Bar"), new Table("lol.kek"), new Table("dbo.stufu") } });
			}
		}
	}
}
