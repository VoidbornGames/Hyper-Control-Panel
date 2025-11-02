import React, { createContext, useContext, useReducer, ReactNode } from 'react';

interface LoadingState {
  [key: string]: boolean;
}

interface LoadingContextType {
  isLoading: (key: string) => boolean;
  setLoading: (key: string, loading: boolean) => void;
  setGlobalLoading: (loading: boolean) => void;
  globalLoading: boolean;
}

type LoadingAction =
  | { type: 'SET_LOADING'; payload: { key: string; loading: boolean } }
  | { type: 'SET_GLOBAL_LOADING'; payload: boolean }
  | { type: 'CLEAR_LOADING'; payload: string };

const initialState: LoadingState = {
  global: false,
};

const loadingReducer = (state: LoadingState, action: LoadingAction): LoadingState => {
  switch (action.type) {
    case 'SET_LOADING':
      return {
        ...state,
        [action.payload.key]: action.payload.loading,
      };
    case 'SET_GLOBAL_LOADING':
      return {
        ...state,
        global: action.payload,
      };
    case 'CLEAR_LOADING':
      const newState = { ...state };
      delete newState[action.payload];
      return newState;
    default:
      return state;
  }
};

interface LoadingProviderProps {
  children: ReactNode;
}

const LoadingContext = createContext<LoadingContextType | undefined>(undefined);

export const LoadingProvider: React.FC<LoadingProviderProps> = ({ children }) => {
  const [state, dispatch] = useReducer(loadingReducer, initialState);

  const isLoading = (key: string): boolean => {
    return state[key] || false;
  };

  const setLoading = (key: string, loading: boolean): void => {
    dispatch({ type: 'SET_LOADING', payload: { key, loading } });
  };

  const setGlobalLoading = (loading: boolean): void => {
    dispatch({ type: 'SET_GLOBAL_LOADING', payload: loading });
  };

  const clearLoading = (key: string): void => {
    dispatch({ type: 'CLEAR_LOADING', payload: key });
  };

  const value: LoadingContextType = {
    isLoading,
    setLoading,
    setGlobalLoading,
    globalLoading: state.global || false,
  };

  return <LoadingContext.Provider value={value}>{children}</LoadingContext.Provider>;
};

export const useLoading = (): LoadingContextType => {
  const context = useContext(LoadingContext);
  if (context === undefined) {
    throw new Error('useLoading must be used within a LoadingProvider');
  }
  return context;
};