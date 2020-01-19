using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Dict = Vostok.Commons.Collections.ImmutableArrayDictionary<string, string>;

// ReSharper disable ReturnValueOfPureMethodIsNotUsed

namespace Vostok.Commons.Collections.Tests
{
    [TestFixture]
    internal class ImmutableArrayDictionary_Tests
    {
        [Test]
        public void Empty_instance_should_not_contain_any_values()
        {
            Dict.Empty.Should().BeEmpty();
        }

        [Test]
        public void Empty_instance_should_have_zero_count()
        {
            Dict.Empty.Count.Should().Be(0);
        }

        [Test]
        public void Should_be_able_to_add_a_value_to_empty_instance()
        {
            var dictionary = Dict.Empty.Set("k", "v");

            dictionary.Count.Should().Be(1);
            dictionary["k"].Should().Be("v");
        }

        [Test]
        public void Should_not_modify_empty_instance_when_deriving_from_it()
        {
            Dict.Empty.Set("k", "v");

            Dict.Empty.Should().BeEmpty();

            Dict.Empty.Count.Should().Be(0);
        }

        [Test]
        public void Should_use_provided_equality_comparer_when_comparing_keys()
        {
            var dictionary = new Dict(1, StringComparer.OrdinalIgnoreCase)
                .Set("key", "value")
                .Set("KEY", "value");

            dictionary.Should().HaveCount(1);

            dictionary.Keys.Should().Equal("key");
        }

        [Test]
        public void Set_should_be_able_to_expand_beyond_initial_capacity()
        {
            var dictionary = new Dict(1)
                .Set("k1", "v1")
                .Set("k2", "v2")
                .Set("k3", "v3")
                .Set("k4", "v4")
                .Set("k5", "v5");

            dictionary.Count.Should().Be(5);

            dictionary.Keys.Should().Equal("k1", "k2", "k3", "k4", "k5");
        }

        [Test]
        public void Set_should_return_same_instance_when_replacing_a_value_with_same_value()
        {
            var dictionaryBefore = Dict.Empty
                .Set("k1", "v1")
                .Set("k2", "v2")
                .Set("k3", "v3");

            var dictionaryAfter = dictionaryBefore.Set("k2", "v2");

            dictionaryAfter.Should().BeSameAs(dictionaryBefore);
        }

        [Test]
        public void Set_should_replace_existing_value_when_provided_with_a_different_value()
        {
            var dictionaryBefore = Dict.Empty
                .Set("k1", "v1")
                .Set("k2", "v2")
                .Set("k3", "v3");

            var dictionaryAfter = dictionaryBefore.Set("k2", "vx");

            dictionaryAfter.Should().NotBeSameAs(dictionaryBefore);
            dictionaryAfter.Count.Should().Be(3);
            dictionaryAfter["k1"].Should().Be("v1");
            dictionaryAfter["k2"].Should().Be("vx");
            dictionaryAfter["k3"].Should().Be("v3");
        }

        [Test]
        public void Set_should_not_overwrite_existing_value_when_it_is_explicitly_prohibited()
        {
            var dictionaryBefore = Dict.Empty
                .Set("k1", "v1")
                .Set("k2", "v2")
                .Set("k3", "v3");

            var dictionaryAfter = dictionaryBefore.Set("k2", "vx", false);

            dictionaryAfter.Should().BeSameAs(dictionaryBefore);
        }

        [Test]
        public void Set_should_not_modify_base_instance_when_deriving_from_it_by_replacing_existing_value()
        {
            var dictionaryBefore = Dict.Empty
                .Set("k1", "v1")
                .Set("k2", "v2")
                .Set("k3", "v3");

            var dictionaryAfter = dictionaryBefore.Set("k2", "vx");

            dictionaryAfter.Should().NotBeSameAs(dictionaryBefore);
            dictionaryBefore.Count.Should().Be(3);
            dictionaryBefore["k1"].Should().Be("v1");
            dictionaryBefore["k2"].Should().Be("v2");
            dictionaryBefore["k3"].Should().Be("v3");
        }

        [Test]
        public void Set_should_be_able_to_grow_new_instance_when_adding_unique_keys_without_spoiling_base_instance()
        {
            var dictionary = new Dict(0);

            for (var i = 0; i < 100; i++)
            {
                var newKey = "key-" + i;
                var newValue = "value-" + i;
                var newDictionary = dictionary.Set(newKey, newValue);

                newDictionary.Should().NotBeSameAs(dictionary);
                newDictionary.Count.Should().Be(i + 1);
                newDictionary[newKey].Should().Be(newValue);

                dictionary.Count.Should().Be(i);
                dictionary.ContainsKey(newKey).Should().BeFalse();
                dictionary = newDictionary;
            }
        }

        [Test]
        public void Set_should_correctly_handle_forking_multiple_instances_from_a_single_base()
        {
            var dictionaryBefore = Dict.Empty
                .Set("k1", "v1")
                .Set("k2", "v2")
                .Set("k3", "v3");

            var dictionaryAfter1 = dictionaryBefore.Set("k4", "v4-1");
            var dictionaryAfter2 = dictionaryBefore.Set("k4", "v4-2");

            dictionaryBefore.Count.Should().Be(3);
            dictionaryBefore.ContainsKey("k4").Should().BeFalse();

            dictionaryAfter1.Should().NotBeSameAs(dictionaryBefore);
            dictionaryAfter1.Count.Should().Be(4);
            dictionaryAfter1["k4"].Should().Be("v4-1");

            dictionaryAfter2.Should().NotBeSameAs(dictionaryBefore);
            dictionaryAfter2.Should().NotBeSameAs(dictionaryAfter1);
            dictionaryAfter2.Count.Should().Be(4);
            dictionaryAfter2["k4"].Should().Be("v4-2");
        }

        [Test]
        public void Remove_should_correctly_remove_first_value()
        {
            var dictionaryBefore = Dict.Empty
                .Set("k1", "v1")
                .Set("k2", "v2")
                .Set("k3", "v3")
                .Set("k4", "v4")
                .Set("k5", "v5");

            var dictionaryAfter = dictionaryBefore.Remove("k1");

            dictionaryAfter.Should().NotBeSameAs(dictionaryBefore);
            dictionaryAfter.Should().HaveCount(4);
            dictionaryAfter.ContainsKey("k1").Should().BeFalse();
        }

        [Test]
        public void Remove_should_correctly_remove_last_value()
        {
            var dictionaryBefore = Dict.Empty
                .Set("k1", "v1")
                .Set("k2", "v2")
                .Set("k3", "v3")
                .Set("k4", "v4")
                .Set("k5", "v5");

            var dictionaryAfter = dictionaryBefore.Remove("k5");

            dictionaryAfter.Should().NotBeSameAs(dictionaryBefore);
            dictionaryAfter.Should().HaveCount(4);
            dictionaryAfter.ContainsKey("k5").Should().BeFalse();
        }

        [Test]
        public void Remove_should_correctly_remove_a_value_from_the_middle()
        {
            var dictionaryBefore = Dict.Empty
                .Set("k1", "v1")
                .Set("k2", "v2")
                .Set("k3", "v3")
                .Set("k4", "v4")
                .Set("k5", "v5");

            var dictionaryAfter = dictionaryBefore.Remove("k3");

            dictionaryAfter.Should().NotBeSameAs(dictionaryBefore);
            dictionaryAfter.Should().HaveCount(4);
            dictionaryAfter.ContainsKey("k3").Should().BeFalse();
        }

        [Test]
        public void Remove_should_correctly_remove_the_only_value_in_collection()
        {
            var dictionaryBefore = Dict.Empty
                .Set("k1", "v1");

            var dictionaryAfter = dictionaryBefore.Remove("k1");

            dictionaryAfter.Should().NotBeSameAs(dictionaryBefore);
            dictionaryAfter.Count.Should().Be(0);
            dictionaryAfter.Should().BeEmpty();
        }

        [Test]
        public void Remove_should_return_same_instance_when_removing_a_value_which_is_not_present()
        {
            var dictionaryBefore = Dict.Empty
                .Set("k1", "v1")
                .Set("k2", "v2")
                .Set("k3", "v3")
                .Set("k4", "v4")
                .Set("k5", "v5");

            var dictionaryAfter = dictionaryBefore.Remove("k6");

            dictionaryAfter.Should().BeSameAs(dictionaryBefore);
        }

        [Test]
        public void Remove_should_not_spoil_base_insance()
        {
            var dictionaryBefore = Dict.Empty
                .Set("k1", "v1")
                .Set("k2", "v2")
                .Set("k3", "v3")
                .Set("k4", "v4")
                .Set("k5", "v5");

            dictionaryBefore.Remove("k1");
            dictionaryBefore.Remove("k2");
            dictionaryBefore.Remove("k3");
            dictionaryBefore.Remove("k4");
            dictionaryBefore.Remove("k5");

            dictionaryBefore.Should().HaveCount(5);
            dictionaryBefore["k1"].Should().Be("v1");
            dictionaryBefore["k2"].Should().Be("v2");
            dictionaryBefore["k3"].Should().Be("v3");
            dictionaryBefore["k4"].Should().Be("v4");
            dictionaryBefore["k5"].Should().Be("v5");
        }

        [Test]
        public void Indexer_should_throw_an_exception_when_dictionary_is_empty()
        {
            Action action = () => Dict.Empty["name"].GetHashCode();

            var error = action.Should().Throw<KeyNotFoundException>().Which;

            Console.Out.WriteLine(error);
        }

        [Test]
        public void Indexer_should_return_null_when_value_with_given_key_does_not_exist()
        {
            var dictionary = Dict.Empty
                .Set("key1", "value1")
                .Set("key2", "value2");

            Action action = () => dictionary["key3"].GetHashCode();

            var error = action.Should().Throw<KeyNotFoundException>().Which;

            Console.Out.WriteLine(error);
        }

        [Test]
        public void Indexer_should_return_correct_values_for_existing_keys()
        {
            var dictionary = Dict.Empty
                .Set("key1", "value1")
                .Set("key2", "value2");

            dictionary["key1"].Should().Be("value1");
            dictionary["key2"].Should().Be("value2");
        }

        [Test]
        public void Indexer_should_use_provided_key_comparer_when_comparing_keys()
        {
            var dictionary = new Dict(StringComparer.OrdinalIgnoreCase)
                .Set("name", "value1")
                .Set("NAME", "value2");

            dictionary["name"].Should().Be("value2");
        }

        [Test]
        public void Copy_constructor_should_copy_all_pairs_from_source()
        {
            var source = new Dict(16)
                .Set("a", "b")
                .Set("c", "d")
                .Set("e", "f");

            var copy = new Dict(source);

            copy.Should().Equal(source);
        }
    }
}