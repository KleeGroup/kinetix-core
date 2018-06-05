﻿using System;
using System.Collections.Generic;
using System.Linq;
using Kinetix.Search.Elastic.Mapping;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Kinetix.Search.Elastic
{
    public class ElasticConfigBuilder
    {
        internal const string ServerName = "Elastic6";

        private readonly IServiceCollection _services;

        internal ElasticConfigBuilder(IServiceCollection services)
        {
            _services = services;
            AddMapping<DateTimeMapping>();
            AddMapping<DecimalMapping>();
            AddMapping<IntMapping>();
            AddMapping<StringMapping>();
        }

        internal ICollection<Type> DocumentTypes { get; } = new List<Type>();

        internal ICollection<JsonConverter> JsonConverters { get; } = new List<JsonConverter>();

        public ElasticConfigBuilder AddDocumentType<T>()
        {
            DocumentTypes.Add(typeof(T));
            return this;
        }

        public ElasticConfigBuilder AddElasticMapping<T>()
            where T : class, IElasticMapping
        {
            return AddMapping<T>();
        }

        public ElasticConfigBuilder AddJsonConverter<T>()
            where T : JsonConverter, new()
        {
            JsonConverters.Add(new T());
            return this;
        }

        private ElasticConfigBuilder AddMapping<T>()
            where T : class, IElasticMapping
        {
            _services.AddSingleton(typeof(T).GetInterfaces().First(), typeof(T));
            return this;
        }
    }
}
