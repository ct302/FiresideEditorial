using FiresideEditorial.Models;

namespace FiresideEditorial.Services;

public interface IRecipeService
{
    Task<List<Recipe>> GetAllRecipesAsync();
    Task<Recipe?> GetRecipeBySlugAsync(string slug);
    Task<List<Recipe>> GetRecipesByCategoryAsync(string category);
}
