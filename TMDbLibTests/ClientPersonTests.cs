﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Person;
using TMDbLibTests.Helpers;

namespace TMDbLibTests
{
    [TestClass]
    public class ClientPersonTests
    {
        private const int BruceWillis = 62;

        private Dictionary<PersonMethods, Func<Person, object>> _methods;
        private TestConfig _config;

        [TestInitialize]
        public void Initiator()
        {
            _config = new TestConfig();

            _methods = new Dictionary<PersonMethods, Func<Person, object>>();
            _methods[PersonMethods.Credits] = movie => movie.Credits;
            _methods[PersonMethods.Changes] = movie => movie.Changes;
            _methods[PersonMethods.Images] = movie => movie.Images;
        }

        [TestMethod]
        public void TestPersonsExtrasNone()
        {
            Person person = _config.Client.GetPerson(BruceWillis);

            Assert.IsNotNull(person);

            // TODO: Test all properties
            Assert.AreEqual("Bruce Willis", person.Name);

            // Test all extras, ensure none of them exist
            foreach (Func<Person, object> selector in _methods.Values)
            {
                Assert.IsNull(selector(person));
            }
        }

        [TestMethod]
        public void TestPersonsLanguage()
        {
            Person person = _config.Client.GetPerson(BruceWillis);
            Person personItalian = _config.Client.GetPerson(BruceWillis, "it");

            Assert.IsNotNull(person);
            Assert.IsNotNull(personItalian);

            Assert.AreEqual("Bruce Willis", person.Name);
            Assert.AreEqual("Bruce Willis", personItalian.Name);

            // Test all extras, ensure none of them exist
            foreach (Func<Person, object> selector in _methods.Values)
            {
                Assert.IsNull(selector(person));
                Assert.IsNull(selector(personItalian));
            }

            // Todo: Check language-specific items
            // Requires a person with alternate names.
        }

        [TestMethod]
        public void TestPersonsExtrasExclusive()
        {
            // Test combinations of extra methods, fetch everything but each one, ensure all but the one exist
            foreach (PersonMethods method in _methods.Keys)
            {
                // Prepare the combination exlcuding the one (method).
                PersonMethods combo = _methods.Keys.Except(new[] { method }).Aggregate((personMethod, accumulator) => personMethod | accumulator);

                // Fetch data
                Person person = _config.Client.GetPerson(BruceWillis, combo);

                // Ensure we have all pieces
                foreach (PersonMethods expectedMethod in _methods.Keys.Except(new[] { method }))
                    Assert.IsNotNull(_methods[expectedMethod](person));

                // .. except the method we're testing.
                Assert.IsNull(_methods[method](person));
            }
        }

        [TestMethod]
        public void TestPersonsGetters()
        {
            //GetPersonCredits(int id, string language)
            {
                Credits resp = _config.Client.GetPersonCredits(BruceWillis);
                Assert.IsNotNull(resp);

                Credits respItalian = _config.Client.GetPersonCredits(BruceWillis, "it");
                Assert.IsNotNull(respItalian);

                Assert.AreEqual(resp.Cast.Count, respItalian.Cast.Count);
                Assert.AreEqual(resp.Crew.Count, respItalian.Crew.Count);
                Assert.AreEqual(resp.Id, respItalian.Id);

                // There must be at least one movie with a different title
                bool allTitlesIdentical = true;
                for (int index = 0; index < resp.Cast.Count; index++)
                {
                    Assert.AreEqual(resp.Cast[index].Id, respItalian.Cast[index].Id);
                    Assert.AreEqual(resp.Cast[index].OriginalTitle, respItalian.Cast[index].OriginalTitle);

                    if (resp.Cast[index].Title != respItalian.Cast[index].Title)
                        allTitlesIdentical = false;
                }

                for (int index = 0; index < resp.Crew.Count; index++)
                {
                    Assert.AreEqual(resp.Crew[index].Id, respItalian.Crew[index].Id);
                    Assert.AreEqual(resp.Crew[index].OriginalTitle, respItalian.Crew[index].OriginalTitle);

                    if (resp.Crew[index].Title != respItalian.Crew[index].Title)
                        allTitlesIdentical = false;
                }

                Assert.IsFalse(allTitlesIdentical);
            }

            //GetPersonChanges(int id, DateTime? startDate = null, DateTime? endDate = null)
            {
                // Find latest changed person
                int latestChanged = _config.Client.GetChangesPeople().Results.First().Id;

                // Fetch changelog
                DateTime lower = DateTime.UtcNow.AddDays(-14);
                DateTime higher = DateTime.UtcNow;
                List<Change> respRange = _config.Client.GetPersonChanges(latestChanged, lower, higher);

                Assert.IsNotNull(respRange);
                Assert.IsTrue(respRange.Count > 0);

                // As TMDb works in days, we need to adjust our values also
                lower = lower.AddDays(-1);
                higher = higher.AddDays(1);

                foreach (Change change in respRange)
                    foreach (ChangeItem changeItem in change.Items)
                    {
                        DateTime date = changeItem.TimeParsed;
                        Assert.IsTrue(lower <= date);
                        Assert.IsTrue(date <= higher);
                    }
            }
        }

        [TestMethod]
        public void TestPersonsImages()
        {
            // Get config
            _config.Client.GetConfig();

            // Test image url generator
            ProfileImages images = _config.Client.GetPersonImages(BruceWillis);

            Assert.AreEqual(BruceWillis, images.Id);
            Assert.IsTrue(images.Profiles.Count > 0);

            List<string> profileSizes = _config.Client.Config.Images.ProfileSizes;

            foreach (Profile profile in images.Profiles)
            {
                foreach (string size in profileSizes)
                {
                    Uri url = _config.Client.GetImageUrl(size, profile.FilePath);
                    Uri urlSecure = _config.Client.GetImageUrl(size, profile.FilePath, true);

                    Assert.IsTrue(TestHelpers.InternetUriExists(url));
                    Assert.IsTrue(TestHelpers.InternetUriExists(urlSecure));
                }
            }
        }
    }
}
