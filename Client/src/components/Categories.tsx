import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { categoryApi } from '../services/api';
import type { CategoryResponseModel, CategoryInputModel } from '../types';

const ITEMS_PER_PAGE = 12;

export const Categories: React.FC = () => {
  const [categories, setCategories] = useState<CategoryResponseModel[]>([]);
  const [displayedCategories, setDisplayedCategories] = useState<CategoryResponseModel[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [categoryName, setCategoryName] = useState('');
  const [categoryColor, setCategoryColor] = useState('#3B82F6');
  const { logout } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    loadCategories();
  }, []);

  useEffect(() => {
    const startIndex = 0;
    const endIndex = currentPage * ITEMS_PER_PAGE;
    setDisplayedCategories(categories.slice(startIndex, endIndex));
  }, [categories, currentPage]);

  const loadCategories = async () => {
    try {
      setLoading(true);
      const data = await categoryApi.getAll();
      setCategories(data);
      setError('');
    } catch (err) {
      setError('Failed to load categories');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateCategory = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const newCategory: CategoryInputModel = {
        name: categoryName,
        color: categoryColor,
      };
      await categoryApi.create(newCategory);
      setIsDialogOpen(false);
      setCategoryName('');
      setCategoryColor('#3B82F6');
      await loadCategories();
    } catch (err) {
      setError('Failed to create category');
      console.error(err);
    }
  };

  const handleDeleteCategory = async (id: string, e: React.MouseEvent) => {
    e.stopPropagation();
    try {
      await categoryApi.delete(id);
      await loadCategories();
    } catch (err) {
      setError('Failed to delete category');
      console.error(err);
    }
  };

  const handleShowMore = () => {
    setCurrentPage((prev) => prev + 1);
  };

  const hasMore = displayedCategories.length < categories.length;

  const getCardStyle = (color?: string) => {
    const bgColor = color || '#6B7280';
    return {
      backgroundColor: bgColor,
      borderColor: bgColor,
    };
  };

  if (loading && categories.length === 0) {
    return (
      <div className="min-h-screen bg-gray-900 flex items-center justify-center">
        <div className="text-white text-xl">Loading categories...</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-900">
      {/* Header */}
      <header className="bg-gray-800 border-b border-gray-700">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex justify-between items-center">
          <h1 className="text-2xl font-bold text-white">Task Planner</h1>
          <div className="flex gap-4 items-center">
            <button
              onClick={() => setIsDialogOpen(true)}
              className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-lg transition"
            >
              Add a category
            </button>
            <button
              onClick={logout}
              className="px-4 py-2 bg-gray-700 hover:bg-gray-600 text-white font-semibold rounded-lg transition"
            >
              Logout
            </button>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {error && (
          <div className="mb-4 p-3 bg-red-900/50 border border-red-700 rounded text-red-200">
            {error}
          </div>
        )}

        {categories.length === 0 ? (
          <div className="text-center py-16">
            <p className="text-gray-400 text-lg mb-4">No categories yet</p>
            <button
              onClick={() => setIsDialogOpen(true)}
              className="px-6 py-3 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-lg transition"
            >
              Create your first category
            </button>
          </div>
        ) : (
          <>
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
              {displayedCategories.map((category) => (
                <div
                  key={category.id}
                  onClick={() => navigate(`/categories/${category.id}/tasks`)}
                  className="relative cursor-pointer rounded-lg p-6 text-white shadow-lg hover:scale-105 transition-transform"
                  style={getCardStyle(category.color)}
                >
                  <button
                    aria-label="Delete category"
                    onClick={(e) => handleDeleteCategory(category.id, e)}
                    className="absolute top-3 right-3 text-white/80 hover:text-white bg-black/20 hover:bg-black/30 rounded-full w-8 h-8 flex items-center justify-center"
                  >
                    ×
                  </button>
                  <h3 className="text-xl font-bold mb-2">{category.name}</h3>
                  <p className="text-sm opacity-90">
                    {category.sortOrder !== undefined && `Order: ${category.sortOrder}`}
                  </p>
                </div>
              ))}
            </div>

            {hasMore && (
              <div className="mt-8 text-center">
                <button
                  onClick={handleShowMore}
                  className="px-6 py-3 bg-gray-700 hover:bg-gray-600 text-white font-semibold rounded-lg transition"
                >
                  Show more
                </button>
              </div>
            )}
          </>
        )}
      </main>

      {/* Add Category Dialog */}
      {isDialogOpen && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-gray-800 rounded-lg shadow-xl p-6 w-full max-w-md">
            <h2 className="text-2xl font-bold text-white mb-4">Add Category</h2>
            <form onSubmit={handleCreateCategory}>
              <div className="space-y-4">
                <div>
                  <label htmlFor="categoryName" className="block text-sm font-medium text-gray-300 mb-1">
                    Name
                  </label>
                  <input
                    id="categoryName"
                    type="text"
                    value={categoryName}
                    onChange={(e) => setCategoryName(e.target.value)}
                    required
                    className="w-full px-4 py-2 bg-gray-700 border border-gray-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                    placeholder="Category name"
                  />
                </div>

                <div>
                  <label htmlFor="categoryColor" className="block text-sm font-medium text-gray-300 mb-1">
                    Color
                  </label>
                  <input
                    id="categoryColor"
                    type="color"
                    value={categoryColor}
                    onChange={(e) => setCategoryColor(e.target.value)}
                    className="w-full h-12 bg-gray-700 border border-gray-600 rounded-lg cursor-pointer"
                  />
                </div>
              </div>

              <div className="mt-6 flex gap-4 justify-end">
                <button
                  type="button"
                  onClick={() => {
                    setIsDialogOpen(false);
                    setCategoryName('');
                    setCategoryColor('#3B82F6');
                  }}
                  className="px-4 py-2 bg-gray-700 hover:bg-gray-600 text-white font-semibold rounded-lg transition"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-lg transition"
                >
                  Create
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
