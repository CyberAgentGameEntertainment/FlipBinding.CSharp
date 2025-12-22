// SPDX-FileCopyrightText: 2025 CyberAgent, Inc.
// SPDX-License-Identifier: MIT

namespace FlipBinding.CSharp.Tests;

public class FlipResultTest
{
    [Test]
    public void GetPixel_ReturnsCorrectValue()
    {
        float[] errorMap = [0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f];
        var result = new FlipResult(0.35f, errorMap, 3, 2, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetPixel(0, 0), Is.EqualTo(0.1f));
            Assert.That(result.GetPixel(2, 0), Is.EqualTo(0.3f));
            Assert.That(result.GetPixel(0, 1), Is.EqualTo(0.4f));
            Assert.That(result.GetPixel(2, 1), Is.EqualTo(0.6f));
        });
    }

    [TestCase(-1, 0)]
    [TestCase(2, 0)]
    [TestCase(0, -1)]
    [TestCase(0, 2)]
    public void GetPixel_ThrowsOnOutOfBounds(int x, int y)
    {
        float[] errorMap = [0.1f, 0.2f, 0.3f, 0.4f];
        var result = new FlipResult(0.25f, errorMap, 2, 2, false);

        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetPixel(x, y));
    }

    [Test]
    public void GetPixel_ThrowsWhenMagmaMap()
    {
        float[] errorMap = [0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f, 0.0f, 0.5f];
        var result = new FlipResult(0.5f, errorMap, 2, 2, true);

        Assert.Throws<InvalidOperationException>(() => result.GetPixel(0, 0));
    }

    [Test]
    public void GetPixel_ThrowsWhenEmpty()
    {
        var result = new FlipResult(0f, [], 2, 2, true);

        Assert.Throws<InvalidOperationException>(() => result.GetPixel(0, 0));
    }

    [Test]
    public void GetPixelRgb_ReturnsCorrectValue()
    {
        // 2x2 Magma map with RGB values (width * height * 3 = 12 elements)
        float[] errorMap =
        [
            0.1f, 0.2f, 0.3f, // (0, 0)
            0.4f, 0.5f, 0.6f, // (1, 0)
            0.7f, 0.8f, 0.9f, // (0, 1)
            1.0f, 0.0f, 0.5f  // (1, 1)
        ];
        var result = new FlipResult(0.5f, errorMap, 2, 2, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetPixelRgb(0, 0), Is.EqualTo((0.1f, 0.2f, 0.3f)));
            Assert.That(result.GetPixelRgb(1, 0), Is.EqualTo((0.4f, 0.5f, 0.6f)));
            Assert.That(result.GetPixelRgb(0, 1), Is.EqualTo((0.7f, 0.8f, 0.9f)));
            Assert.That(result.GetPixelRgb(1, 1), Is.EqualTo((1.0f, 0.0f, 0.5f)));
        });
    }

    [TestCase(-1, 0)]
    [TestCase(2, 0)]
    [TestCase(0, -1)]
    [TestCase(0, 2)]
    public void GetPixelRgb_ThrowsOnOutOfBounds(int x, int y)
    {
        float[] errorMap = [0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f, 0.0f, 0.5f];
        var result = new FlipResult(0.5f, errorMap, 2, 2, true);

        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetPixelRgb(x, y));
    }

    [Test]
    public void GetPixelRgb_ThrowsWhenNotMagmaMap()
    {
        float[] errorMap = [0.1f, 0.2f, 0.3f, 0.4f];
        var result = new FlipResult(0.25f, errorMap, 2, 2, false);

        Assert.Throws<InvalidOperationException>(() => result.GetPixelRgb(0, 0));
    }

    [Test]
    public void GetPixelRgb_ThrowsWhenEmpty()
    {
        var result = new FlipResult(0f, [], 2, 2, true);

        Assert.Throws<InvalidOperationException>(() => result.GetPixelRgb(0, 0));
    }

    [Test]
    public void HasErrorMap_Empty_ReturnsFalse()
    {
        var result = new FlipResult(0f, [], 2, 2, true);
        Assert.That(result.HasErrorMap, Is.False);
    }

    [Test]
    public void HasErrorMap_NotEmpty_ReturnsTrue()
    {
        float[] errorMap = [0.1f, 0.2f, 0.3f, 0.4f];
        var result = new FlipResult(0.25f, errorMap, 2, 2, false);
        Assert.That(result.HasErrorMap, Is.True);
    }
}
