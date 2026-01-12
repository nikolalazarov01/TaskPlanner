// test/helpers/router.tsx
import React from 'react';
import { MemoryRouter, Routes, Route, useNavigate, useLocation } from 'react-router-dom';

// Component to capture router state
function RouterStateCapture({ onStateChange }: { onStateChange: (state: any) => void }) {
  const navigate = useNavigate();
  const location = useLocation();
  
  React.useEffect(() => {
    onStateChange({ navigate, location });
  }, [navigate, location, onStateChange]);
  
  return null;
}

export function renderWithRouter(ui: React.ReactNode, initialPath = '/categories/1/tasks') {
  let routerState: { navigate: any; location: any } | null = null;
  
  const element = (
    <MemoryRouter initialEntries={[initialPath]}>
      <RouterStateCapture onStateChange={(state) => { routerState = state; }} />
      <Routes>
        <Route path="/categories" element={<div>Categories Page</div>} />
        <Route path="/categories/:categoryId/tasks" element={ui} />
      </Routes>
    </MemoryRouter>
  );
  
  return {
    element,
    getRouter: () => routerState,
  };
}