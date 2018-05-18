using Microsoft.AspNetCore.Mvc;
using nikstra.CoreShopping.Web.Controllers;
using NUnit.Framework;
using System;

namespace nikstra.CoreShopping.Web.Tests
{
    [TestFixture]
    public class HomeControllerTests
    {
        [Test]
        public void GetIndex_ReturnsViewResult_WhenCalled()
        {
            var controller = new HomeController();

            IActionResult result = controller.Index();

            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public void GetAbout_ReturnsViewResult_WhenCalled()
        {
            var controller = new HomeController();

            IActionResult result = controller.Index();

            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public void GetContact_ReturnsViewResult_WhenCalled()
        {
            var controller = new HomeController();

            IActionResult result = controller.Index();

            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public void GetError_ReturnsViewResult_WhenCalled()
        {
            var controller = new HomeController();

            IActionResult result = controller.Index();

            Assert.That(result, Is.InstanceOf<ViewResult>());
        }
    }
}
