import { configureStore } from '@reduxjs/toolkit';

// Define the root state type
export interface RootState {
  // Add your reducers here
}

// Create the store
export const store = configureStore({
  reducer: {
    // Add your reducers here
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: {
        ignoredActions: ['persist/PERSIST'],
      },
    }),
});

export type AppDispatch = typeof store.dispatch;