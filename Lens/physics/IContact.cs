namespace Lens.physics;

public interface IContact {
	IFixture FixtureA { get; }
	IFixture FixtureB { get; }
	bool Enabled { get; set; }
}
