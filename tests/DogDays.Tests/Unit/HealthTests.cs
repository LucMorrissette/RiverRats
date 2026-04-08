using DogDays.Components;
using DogDays.Tests.Helpers;

namespace DogDays.Tests.Unit;

public class HealthTests
{
    [Fact]
    public void TakeDamage__ReducesCurrentHp__ByDamageAmount()
    {
        var health = new Health(10);

        health.TakeDamage(3);

        Assert.Equal(7, health.CurrentHp);
    }

    [Fact]
    public void TakeDamage__ClampsToZero__WhenDamageExceedsHp()
    {
        var health = new Health(5);

        health.TakeDamage(20);

        Assert.Equal(0, health.CurrentHp);
    }

    [Fact]
    public void TakeDamage__FiresOnDamaged__WithDamageAmount()
    {
        var health = new Health(10);
        int reported = -1;
        health.OnDamaged += amount => reported = amount;

        health.TakeDamage(4);

        Assert.Equal(4, reported);
    }

    [Fact]
    public void TakeDamage__FiresOnDied__WhenHpReachesZero()
    {
        var health = new Health(3);
        bool died = false;
        health.OnDied += () => died = true;

        health.TakeDamage(3);

        Assert.True(died);
    }

    [Fact]
    public void TakeDamage__DoesNothing__WhenInvincible()
    {
        var health = new Health(10);
        health.SetInvincible(true);

        health.TakeDamage(5);

        Assert.Equal(10, health.CurrentHp);
    }

    [Fact]
    public void TakeDamage__DoesNothing__WhenAlreadyDead()
    {
        var health = new Health(5);
        health.TakeDamage(5);
        int damageEvents = 0;
        health.OnDamaged += _ => damageEvents++;

        health.TakeDamage(1);

        Assert.Equal(0, health.CurrentHp);
        Assert.Equal(0, damageEvents);
    }

    [Fact]
    public void Heal__IncreasesHp__ByHealAmount()
    {
        var health = new Health(10);
        health.TakeDamage(6);

        health.Heal(3);

        Assert.Equal(7, health.CurrentHp);
    }

    [Fact]
    public void Heal__ClampsToMaxHp__WhenHealExceedsMax()
    {
        var health = new Health(10);
        health.TakeDamage(2);

        health.Heal(100);

        Assert.Equal(10, health.CurrentHp);
    }

    [Fact]
    public void Heal__DoesNothing__WhenDead()
    {
        var health = new Health(5);
        health.TakeDamage(5);

        health.Heal(3);

        Assert.Equal(0, health.CurrentHp);
    }

    [Fact]
    public void IncreaseMax__IncreasesMaxAndCurrentHp__ByAmount()
    {
        var health = new Health(10);

        health.IncreaseMax(5);

        Assert.Equal(15, health.MaxHp);
        Assert.Equal(15, health.CurrentHp);
    }

    [Fact]
    public void SetInvincibleForDuration__ClearsAfterDuration__WhenUpdated()
    {
        var health = new Health(10);
        health.SetInvincibleForDuration(1.0f);

        Assert.True(health.IsInvincible);

        // Advance 0.5s — still invincible
        health.Update(FakeGameTime.FromSeconds(0.5f));
        Assert.True(health.IsInvincible);

        // Advance another 0.6s — timer expired
        health.Update(FakeGameTime.FromSeconds(0.6f));
        Assert.False(health.IsInvincible);
    }

    [Fact]
    public void IsAlive__ReturnsTrue__WhenHpAboveZero()
    {
        var health = new Health(1);

        Assert.True(health.IsAlive);
    }

    [Fact]
    public void IsAlive__ReturnsFalse__WhenHpIsZero()
    {
        var health = new Health(1);
        health.TakeDamage(1);

        Assert.False(health.IsAlive);
    }
}
