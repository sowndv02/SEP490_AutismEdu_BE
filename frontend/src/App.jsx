import { BrowserRouter, Route, Routes } from "react-router-dom";
import AdminLayout from "./layout/AdminLayout";
import { adminRoutes, publicRoutes } from "./routes/Routes";

function App() {
  return (
    <>
      <BrowserRouter>
        <Routes>
          {publicRoutes.map((route) => (
            <Route
              key={route.path}
              path={route.path}
              element={<route.element />}
            />
          ))}
          <Route path="/admin" element={<AdminLayout />}>
            {adminRoutes.map((route) => (
              <Route
                key={route.path}
                path={route.path}
                element={<route.element/>}
              />
            ))}
          </Route>
        </Routes>
      </BrowserRouter>
    </>
  )
}

export default App
