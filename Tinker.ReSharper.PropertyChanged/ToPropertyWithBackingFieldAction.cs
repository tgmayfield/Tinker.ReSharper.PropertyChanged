using System;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
using JetBrains.ReSharper.Intentions.Extensibility;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Naming.Extentions;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace Tinker.ReSharper.PropertyChanged
{
	[ContextAction(
		Name = "ToPropertyWithBackingFieldAction",
		Description = "Switches automatic properties to implement INotifyPropertyChanged with a backing field",
		Group = "C#")]
	public class ToPropertyWithBackingFieldAction : ContextActionBase
	{
		private readonly ICSharpContextActionDataProvider _provider;

		public ToPropertyWithBackingFieldAction(ICSharpContextActionDataProvider provider)
		{
			_provider = provider;
		}

		public override bool IsAvailable(IUserDataHolder cache)
		{
			var propertyDeclaration = _provider.GetSelectedElement<IPropertyDeclaration>(false, true);
			if (propertyDeclaration == null || propertyDeclaration.IsStatic || propertyDeclaration.IsAbstract || propertyDeclaration.IsOverride)
			{
				return false;
			}

			if (!propertyDeclaration.IsAuto)
			{
				return false;
			}

			var typeDeclaration = propertyDeclaration.GetContainingTypeDeclaration();
			if (typeDeclaration.DeclaredElement == null)
			{
				return false;
			}

			var notifyPropertyChanged = TypeFactory.CreateTypeByCLRName("System.ComponentModel.INotifyPropertyChanged", propertyDeclaration.DeclaredElement.Module, propertyDeclaration.DeclaredElement.ResolveContext);
			bool isNotifyPropertyChanged = typeDeclaration.DeclaredElement.IsDescendantOf(notifyPropertyChanged.GetTypeElement());
			return isNotifyPropertyChanged;
		}

		protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
		{
			var property = _provider.GetSelectedElement<IPropertyDeclaration>(false, true);
			var propertyClass = ((IClassLikeDeclaration)ClassLikeDeclarationNavigator.GetByPropertyDeclaration(property));
			if (property == null || propertyClass == null)
			{
				return null;
			}

			string propertyChangedMethod = "RaisePropertyChanged";
			var parentType = property.GetContainingTypeDeclaration();
			var members = new List<IDeclaration>(parentType.MemberDeclarations);
			foreach (var super in parentType.SuperTypes)
			{
				members.AddRange(super.GetTypeElement().GetMembers().SelectMany(m => m.GetDeclarations()));
			}

			foreach (var member in members.OfType<IMethodDeclaration>())
			{
				if (member.DeclaredName.Contains("PropertyChanged"))
				{
					propertyChangedMethod = member.DeclaredName;
				}
			}

			CSharpElementFactory factory = CSharpElementFactory.GetInstance(property.GetPsiModule());

			var field = propertyClass.AddClassMemberDeclarationBefore((IFieldDeclaration)factory.CreateTypeMemberDeclaration("private $0 field;", property.DeclaredElement.Type), property);
			field.SetName(GetFieldName(property));

			foreach (IAccessorDeclaration accessorDeclaration in property.AccessorDeclarations)
			{
				accessorDeclaration.SetBody(factory.CreateEmptyBlock());

				switch (accessorDeclaration.Kind)
				{
					case AccessorKind.GETTER:
						accessorDeclaration.Body.AddStatementAfter(factory.CreateStatement("return $0;", field.DeclaredElement), null);
						break;

					case AccessorKind.SETTER:
						ICSharpStatement previous = accessorDeclaration.Body.AddStatementAfter(factory.CreateStatement("if ($0 == value) return;", field.DeclaredElement), null);
						previous = accessorDeclaration.Body.AddStatementAfter(factory.CreateStatement("$0 = value;", field.DeclaredElement), previous);
						accessorDeclaration.Body.AddStatementAfter(factory.CreateStatement(propertyChangedMethod + "(\"$0\");", property.NameIdentifier.Name), previous);
						break;
				}
			}

			var formatter = CSharpLanguage.Instance.LanguageServiceNotNull().CodeFormatter;
			if (formatter != null)
			{
				formatter.Format(field.GetPreviousToken(), property.LastChild);
			}
			return null;
		}

		private static string GetFieldName(IPropertyDeclaration property)
		{
			var nameSuggestionManager = property.GetPsiServices().Naming.Suggestion;

			return nameSuggestionManager.GetDerivedName(property.DeclaredElement, NamedElementKinds.PrivateInstanceFields, ScopeKind.Common, property.Language, new SuggestionOptions(), property.GetSourceFile());
		}

		public override string Text
		{
			get { return "To property with backing field (INotifyPropertyChanged)"; }
		}
	}
}