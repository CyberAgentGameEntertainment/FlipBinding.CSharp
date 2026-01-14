// SPDX-FileCopyrightText: 2026 CyberAgent, Inc.
// SPDX-License-Identifier: MIT

// Test cases ported from flip/src/tests/test.py
// Original copyright: (c) 2020-2024, NVIDIA CORPORATION & AFFILIATES. All rights reserved.
// SPDX-License-Identifier: BSD-3-Clause

namespace FlipBinding.CSharp.Tests;

/// <summary>
/// Integration tests for FLIP evaluation using test images from the flip submodule.
/// These tests are ported from the original Python test suite.
/// </summary>
[TestFixture]
public class FlipTest
{
    // Expected mean values from flip/src/tests/test.py
    private const float ExpectedLdrMean = 0.159691f;
    private const float ExpectedHdrMean = 0.283478f;

    // Tolerance for floating point comparison
    private const float MeanTolerance = 1E-05f;

    private string _referencePngPath = null!;
    private string _testPngPath = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _referencePngPath = TestImageLoader.GetImagePath("reference.png");
        _testPngPath = TestImageLoader.GetImagePath("test.png");

        // Verify test images exist
        Assume.That(File.Exists(_referencePngPath), Is.True,
            $"Reference image not found at: {_referencePngPath}. Make sure the flip submodule is initialized.");
        Assume.That(File.Exists(_testPngPath), Is.True,
            $"Test image not found at: {_testPngPath}. Make sure the flip submodule is initialized.");
    }

    [Test]
    [Description("Tests LDR-FLIP mean value matches expected result from original test suite")]
    public void Evaluate_WithLdrTestImages_ReturnsMeanCloseToExpected()
    {
        // Arrange
        var (referenceData, width, height) = TestImageLoader.LoadPngAsRgbFloat(_referencePngPath);
        var (testData, testWidth, testHeight) = TestImageLoader.LoadPngAsRgbFloat(_testPngPath);

        Assume.That(testWidth, Is.EqualTo(width), "Image widths must match");
        Assume.That(testHeight, Is.EqualTo(height), "Image heights must match");

        // Act
        var result = Flip.Evaluate(referenceData, testData, width, height);

        // Assert
        Assert.That(result.MeanError, Is.EqualTo(ExpectedLdrMean).Within(MeanTolerance));
    }

    [Test]
    [Description("Tests that comparing identical images returns zero error")]
    public void Evaluate_WithIdenticalImages_ReturnsZeroMean()
    {
        // Arrange
        var (referenceData, width, height) = TestImageLoader.LoadPngAsRgbFloat(_referencePngPath);

        // Act
        var result = Flip.Evaluate(referenceData, referenceData, width, height);

        // Assert
        Assert.That(result.MeanError, Is.EqualTo(0f));
    }

    [Test]
    [Description("Tests that error map dimensions match input image dimensions")]
    public void Evaluate_WithTestImages_ReturnsCorrectDimensions()
    {
        // Arrange
        var (referenceData, width, height) = TestImageLoader.LoadPngAsRgbFloat(_referencePngPath);
        var (testData, _, _) = TestImageLoader.LoadPngAsRgbFloat(_testPngPath);

        // Act
        var result = Flip.Evaluate(referenceData, testData, width, height);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.Width, Is.EqualTo(width));
            Assert.That(result.Height, Is.EqualTo(height));
            Assert.That(result.IsMagmaMap, Is.False);
            Assert.That(result.ErrorMap, Has.Length.EqualTo(width * height));
        });
    }

    [Test]
    [Description("Tests that error map dimensions match input image dimensions")]
    public void Evaluate_WithMagmaMap_ReturnsCorrectDimensions()
    {
        // Arrange
        var (referenceData, width, height) = TestImageLoader.LoadPngAsRgbFloat(_referencePngPath);
        var (testData, _, _) = TestImageLoader.LoadPngAsRgbFloat(_testPngPath);

        // Act
        var result = Flip.Evaluate(referenceData, testData, width, height, applyMagmaMap: true);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.Width, Is.EqualTo(width));
            Assert.That(result.Height, Is.EqualTo(height));
            Assert.That(result.IsMagmaMap, Is.True);
            Assert.That(result.ErrorMap, Has.Length.EqualTo(width * height * 3));
        });
    }

    [Test]
    [Description("Tests that Magma map output matches the reference image from FLIP test suite")]
    public void Evaluate_WithMagmaMap_MatchesReferenceImage()
    {
        // Arrange
        var (referenceData, width, height) = TestImageLoader.LoadPngAsRgbFloat(_referencePngPath);
        var (testData, _, _) = TestImageLoader.LoadPngAsRgbFloat(_testPngPath);

        var correctImagePath = TestImageLoader.GetTestReferencePath("correct_ldrflip_cpp.png");
        Assume.That(File.Exists(correctImagePath), Is.True,
            $"Correct FLIP image not found at: {correctImagePath}");

        var (expectedData, expectedWidth, expectedHeight) = TestImageLoader.LoadPngAsRgbFloatSrgb(correctImagePath);

        // Act
        var result = Flip.Evaluate(referenceData, testData, width, height, applyMagmaMap: true);

        // Save error map to PNG for debugging
        var outputDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestOutputs");
        Directory.CreateDirectory(outputDir);
        var outputPath = Path.Combine(outputDir, TestContext.CurrentContext.Test.Name + ".png");
        TestImageLoader.SaveRgbFloatAsPng(result.ErrorMap, result.Width, result.Height, outputPath);

        // Compare pixel values with tolerance (allow for minor floating point differences)
        for (var i = 0; i < result.ErrorMap.Length; i++)
        {
            Assert.That(result.ErrorMap[i], Is.EqualTo(expectedData[i]).Within(0.02f), $"Mismatch at index {i}");
        }
    }

    [Test]
    public void Evaluate_ThrowsOnNullReference()
    {
        var test = new float[12];

        Assert.Throws<ArgumentNullException>(() => Flip.Evaluate(null!, test, 2, 2));
    }

    [Test]
    public void Evaluate_ThrowsOnNullTest()
    {
        var reference = new float[12];

        Assert.Throws<ArgumentNullException>(() => Flip.Evaluate(reference, null!, 2, 2));
    }

    [TestCase(0, 2)]
    [TestCase(2, 0)]
    [TestCase(-1, 2)]
    public void Evaluate_ThrowsOnInvalidDimensions(int with, int height)
    {
        var reference = new float[12];
        var test = new float[12];

        Assert.Throws<ArgumentException>(() => Flip.Evaluate(reference, test, with, height));
    }

    [Test]
    public void Evaluate_ThrowsOnSizeMismatch()
    {
        var reference = new float[12]; // 2x2x3 = 12
        var test = new float[12];

        // Expect 3x3x3 = 27, but arrays are 12
        Assert.Throws<ArgumentException>(() => Flip.Evaluate(reference, test, 3, 3));
    }

    [Test]
    public void CalculatePpd_DefaultParameters_Returns67()
    {
        var ppd = Flip.CalculatePpd();

        Assert.That(ppd, Is.EqualTo(67.02f).Within(0.005f));
    }
}
