﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Kinetix.Caching;
using Kinetix.ComponentModel;
using Kinetix.ComponentModel.Annotations;
using Kinetix.Services.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Autofac;

namespace Kinetix.Services
{
    /// <summary>
    /// Gestionnaire des données de références.
    /// </summary>
    public class ReferenceManager : IReferenceManager
    {
        private const string ReferenceCache = "ReferenceLists";
        private const string StaticCache = "StaticLists";

        private readonly BeanDescriptor _beanDescriptor;
        private readonly CacheManager _cacheManager;
        private readonly IComponentContext _componentContext;
        private readonly ILogger<ReferenceManager> _logger;

        private readonly IDictionary<string, Accessor> _referenceAccessors = new Dictionary<string, Accessor>();

        /// <summary>
        /// Constructeur.
        /// </summary>
        /// <param name="provider">Service provider.</param>
        public ReferenceManager(IComponentContext cc)
        {
            _beanDescriptor = cc.Resolve<BeanDescriptor>();
            _cacheManager = cc.Resolve<CacheManager>();
            _logger = cc.Resolve<ILogger<ReferenceManager>>();
            _componentContext = cc;
        }

        /// <inheritdoc cref="IReferenceManager.FlushCache" />
        public void FlushCache(Type referenceType = null, string referenceName = null)
        {
            if (referenceType == null)
            {
                _cacheManager.GetCache(StaticCache).RemoveAll();
                _cacheManager.GetCache(ReferenceCache).RemoveAll();
            }
            else
            {
                var region = GetCacheRegionByType(referenceType);
                _cacheManager.GetCache(region).Remove(referenceName ?? referenceType.FullName);
            }
        }

        /// <inheritdoc cref="IReferenceManager.GetReferenceList{TReferenceType}(string)" />
        public ICollection<TReferenceType> GetReferenceList<TReferenceType>(string referenceName = null)
            where TReferenceType : new()
        {
            return GetReferenceEntry<TReferenceType>(referenceName).List;
        }

        /// <inheritdoc cref="IReferenceManager.GetReferenceList{TReferenceType}(Func{TReferenceType, bool}, string)" />
        public ICollection<TReferenceType> GetReferenceList<TReferenceType>(Func<TReferenceType, bool> predicate, string referenceName = null)
            where TReferenceType : new()
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return GetReferenceList<TReferenceType>(referenceName)
                .Where(predicate)
                .ToList();
        }

        /// <inheritdoc cref="IReferenceManager.GetReferenceListByCriteria" />
        public ICollection<TReferenceType> GetReferenceListByCriteria<TReferenceType>(TReferenceType criteria)
            where TReferenceType : new()
        {

            var beanColl = new List<TReferenceType>();
            var beanPropertyDescriptorList =
                _beanDescriptor.GetDefinition(criteria).Properties
                    .Where(property => property.GetValue(criteria) != null);

            foreach (TReferenceType bean in GetReferenceList<TReferenceType>())
            {
                bool add = true;
                foreach (var property in beanPropertyDescriptorList)
                {
                    if (property.PrimitiveType == null)
                    {
                        continue;
                    }

                    if (!property.GetValue(criteria).Equals(property.GetValue(bean)))
                    {
                        add = false;
                        break;
                    }
                }

                if (add)
                {
                    beanColl.Add(bean);
                }
            }

            return beanColl;
        }

        /// <inheritdoc cref="IReferenceManager.GetReferenceListByPrimaryKeyList" />
        public ICollection<TReferenceType> GetReferenceListByPrimaryKeyList<TReferenceType>(params object[] primaryKeyArray)
            where TReferenceType : new()
        {
            if (primaryKeyArray == null)
            {
                throw new ArgumentNullException(nameof(primaryKeyArray));
            }

            var referenceType = typeof(TReferenceType);

            var primaryKeyList = new ArrayList(primaryKeyArray);
            var definition = _beanDescriptor.GetDefinition(referenceType);

            var initialList = GetReferenceList<TReferenceType>();

            var dictionnary = new Dictionary<int, TReferenceType>();
            foreach (var item in initialList)
            {
                var primaryKey = definition.PrimaryKey.GetValue(item);
                if (primaryKeyList.Contains(primaryKey))
                {
                    dictionnary.Add(primaryKeyList.IndexOf(primaryKey), item);
                }
            }

            var finalList = new List<TReferenceType>();
            for (int index = 0; index < primaryKeyList.Count; ++index)
            {
                finalList.Add(dictionnary[index]);
            }

            return finalList;
        }

        /// <inheritdoc cref="IReferenceManager.GetReferenceObject" />
        public TReferenceType GetReferenceObject<TReferenceType>(Func<TReferenceType, bool> predicate, string referenceName = null)
            where TReferenceType : new()
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return GetReferenceList<TReferenceType>(referenceName)
                .Single(predicate);
        }

        /// <inheritdoc cref="IReferenceManager.GetReferenceObjectByPrimaryKey" />
        public TReferenceType GetReferenceObjectByPrimaryKey<TReferenceType>(object primaryKey)
            where TReferenceType : new()
        {
            if (primaryKey == null)
            {
                return default(TReferenceType);
            }

            for (int i = 0; i < 2; ++i)
            {
                try
                {
                    var entry = GetReferenceEntry<TReferenceType>();
                    return entry.GetReferenceValue(primaryKey);
                }
                catch (KeyNotFoundException e)
                {
                    if (i == 1)
                    {
                        throw new KeyNotFoundException(e.Message, e);
                    }
                }
            }

            throw new KeyNotFoundException();
        }

        /// <inheritdoc cref="IReferenceManager.GetReferenceValueByPrimaryKey{TReferenceType}(object, Expression{Func{TReferenceType, object}})" />
        public string GetReferenceValueByPrimaryKey<TReferenceType>(object primaryKey, Expression<Func<TReferenceType, object>> propertySelector)
            where TReferenceType : new()
        {
            if (primaryKey == null)
            {
                throw new ArgumentNullException(nameof(primaryKey));
            }

            if (propertySelector == null)
            {
                throw new ArgumentNullException(nameof(propertySelector));
            }

            if (propertySelector.Body is MemberExpression mb && mb.Member != null)
            {
                return GetReferenceValueByPrimaryKey<TReferenceType>(primaryKey, mb.Member.Name);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <inheritdoc cref="IReferenceManager.GetReferenceValueByPrimaryKey{TReferenceType}(object, string)" />
        public string GetReferenceValueByPrimaryKey<TReferenceType>(object primaryKey, string defaultPropertyName = null)
            where TReferenceType : new()
        {
            var reference = GetReferenceObjectByPrimaryKey<TReferenceType>(primaryKey);
            var definition = _beanDescriptor.GetDefinition(reference);
            var property = string.IsNullOrEmpty(defaultPropertyName) ? definition.DefaultProperty : definition.Properties[defaultPropertyName];
            return property.ConvertToString(property.GetValue(reference));
        }

        /// <inheritdoc cref="IReferenceManager.RegisterAccessors" />
        public void RegisterAccessors(Type contractType)
        {
            var contractMethods = contractType.GetMethods();
            for (int i = 0; i < contractMethods.Length; i++)
            {
                MethodInfo method = contractMethods[i];
                Type returnType = method.ReturnType;

                var referenceArray = method.GetCustomAttributes(typeof(ReferenceAccessorAttribute), true);
                if (referenceArray.Length > 0)
                {
                    var attribute = (ReferenceAccessorAttribute)referenceArray[0];
                    CheckGenericType(method, returnType, 0);
                    var accessor = new Accessor(contractType, method, returnType.GetGenericArguments()[0], attribute.Name);
                    var name = accessor.Name ?? accessor.ReferenceType.FullName;
                    if (_referenceAccessors.ContainsKey(name))
                    {
                        throw new NotSupportedException();
                    }

                    _referenceAccessors.Add(name, accessor);
                }
            }
        }

        /// <summary>
        /// Construit l'entrée pour le cache de référence.
        /// </summary>
        /// <returns>Entrée du cache.</returns>
        private ReferenceEntry<TReferenceType> BuildReferenceEntry<TReferenceType>(string referenceName = null)
            where TReferenceType : new()
        {
            var referenceList = InvokeReferenceAccessor<TReferenceType>(referenceName);
            return new ReferenceEntry<TReferenceType>(referenceList, _beanDescriptor.GetDefinition(typeof(TReferenceType)));
        }

        /// <summary>
        /// Vérifie que la méthode retourne une collection d'un type non générique.
        /// </summary>
        /// <param name="method">Méthode.</param>
        /// <param name="returnType">Type retourné par la méthode.</param>
        /// <param name="parameterCount">Nombre de paramètres nécessaire pour la méthode.</param>
        private void CheckGenericType(MethodInfo method, Type returnType, int parameterCount)
        {
            if (!returnType.IsGenericType || (
                    !typeof(ICollection<>).Equals(returnType.GetGenericTypeDefinition()) &&
                    returnType.GetGenericTypeDefinition().GetInterface(typeof(ICollection<>).Name) == null))
            {
                throw new NotSupportedException("SR.ExceptionAccessorMustReturnCollection + returnType.Name");
            }

            if (method.GetParameters().Length != parameterCount)
            {
                throw new NotSupportedException("SR.ExceptionAccessorWithParameters");
            }

            if (returnType.GetGenericArguments().Length > 1)
            {
                throw new NotSupportedException("SR.ExceptionAccessorWithTooManyGenericArgs");
            }
        }

        /// <summary>
        /// Retourne le cache d'un type de reference.
        /// </summary>
        /// <param name="referenceType">Type de reference traité.</param>
        /// <returns>Le nom de la région du cache associé.</returns>
        private string GetCacheRegionByType(Type referenceType)
        {
            var attrs = referenceType.GetCustomAttributes(typeof(ReferenceAttribute), false);
            if (attrs.Length == 0)
            {
                throw new NotSupportedException("Le type " + referenceType + " n'est pas une liste de référence.");
            }

            return ((ReferenceAttribute)attrs[0]).IsStatic ? StaticCache : ReferenceCache;
        }

        /// <summary>
        /// Retourne l'entrée du cache pour le type de référence.
        /// </summary>
        /// <param name="referenceName">Nom de la liste à utiliser.</param>
        /// <returns>Entrée du cache.</returns>
        private ReferenceEntry<TReferenceType> GetReferenceEntry<TReferenceType>(string referenceName = null)
            where TReferenceType : new()
        {
            var region = GetCacheRegionByType(typeof(TReferenceType));
            ReferenceEntry<TReferenceType> entry = null;

            var cache = _cacheManager.GetCache(region);

            if (cache == null)
            {
                entry = BuildReferenceEntry<TReferenceType>(referenceName);
                _logger.LogWarning("Impossible d'établir une connexion avec le cache, la valeur est cherchée en base");
            }
            else
            {
                var element = cache.Get(referenceName ?? typeof(TReferenceType).FullName);
                if (element != null)
                {
                    entry = element.Value as ReferenceEntry<TReferenceType>;
                }
            }

            if (entry == null)
            {
                entry = BuildReferenceEntry<TReferenceType>(referenceName);
                _cacheManager.GetCache(region).Put(new Element(referenceName ?? typeof(TReferenceType).FullName, entry));
            }

            return entry;
        }

        /// <summary>
        /// Retourne la liste de référence du type referenceType.
        /// </summary>
        /// <param name="referenceName">Nom de la liste à utiliser.</param>
        /// <returns>Liste de référence.</returns>
        private ICollection<TReferenceType> InvokeReferenceAccessor<TReferenceType>(string referenceName = null)
        {
            if (!_referenceAccessors.ContainsKey(typeof(TReferenceType).FullName))
            {
                throw new ArgumentException("Pas d'accesseur disponible pour le type " + typeof(TReferenceType).Name);
            }

            var accessor = _referenceAccessors[referenceName ?? typeof(TReferenceType).FullName];

            var service = _componentContext.Resolve(accessor.ContractType);
            var list = accessor.Method.Invoke(service, null);

            var coll = list as ICollection<TReferenceType>;
            if (coll == null)
            {
                throw new ArgumentException(list.GetType().Name);
            }

            return coll;
        }
    }
}
