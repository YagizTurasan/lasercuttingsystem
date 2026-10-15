using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.Extensions
{
    public static class AutoMapperExtensions
    {
        public static IMappingExpression<TSource, TDestination> IgnoreUnmapped<TSource, TDestination>(
            this IMappingExpression<TSource, TDestination> map)
        {
            var destinationProperties = typeof(TDestination)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .Select(p => p.Name);

            foreach (var property in destinationProperties)
            {
                map.ForMember(property, opt => opt.Ignore());
            }

            return map;
        }
    }
}
