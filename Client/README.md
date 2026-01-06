# Task Planner Client

React TypeScript client application for the Task Planner API.

## Features

- 🔐 User authentication (Login/Register)
- 📁 Category management with color-coded cards
- ✅ Task management with sorting and grouping
- 🌙 Dark theme UI
- 📱 Responsive design

## Getting Started

### Prerequisites

- Node.js (v18 or higher)
- npm or yarn
- The Task Planner API running on `http://localhost:5099`

### Installation

1. Install dependencies:
```bash
npm install
```

2. Start the development server:
```bash
npm run dev
```

The application will be available at `http://localhost:5173`

### Building for Production

```bash
npm run build
```

The built files will be in the `dist` directory.

## API Configuration

The application is configured to use the API at `http://localhost:5099` by default. This can be changed by:

1. Setting the `VITE_API_URL` environment variable, or
2. Modifying the proxy settings in `vite.config.ts`

### CORS Configuration

If you're running the client and API on different ports in production, you'll need to configure CORS in the API's `Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// In the pipeline:
app.UseCors("AllowClient");
```

## Project Structure

```
src/
  components/      # React components
    Login.tsx      # Login form
    Register.tsx   # Registration form
    Categories.tsx # Categories page with card grid
    Tasks.tsx      # Tasks page with table view
  contexts/        # React contexts
    AuthContext.tsx # Authentication context
  services/        # API services
    api.ts         # API client and endpoints
  types/           # TypeScript type definitions
    index.ts       # All type definitions
  App.tsx          # Main app component with routing
  main.tsx         # Entry point
  index.css        # Global styles
```

## Usage

1. **Register/Login**: Create an account or login with existing credentials
2. **View Categories**: See all your categories as color-coded cards
3. **Create Category**: Click "Add a category" button to create a new category
4. **View Tasks**: Click on a category card to view its tasks
5. **Sort Tasks**: Use the sort buttons to organize tasks by deadline or estimation
6. **Group Tasks**: Toggle "Group by Priority" to organize tasks by priority level

## Technology Stack

- **React 18** - UI framework
- **TypeScript** - Type safety
- **React Router** - Client-side routing
- **Axios** - HTTP client
- **Tailwind CSS** - Styling
- **Vite** - Build tool and dev server
