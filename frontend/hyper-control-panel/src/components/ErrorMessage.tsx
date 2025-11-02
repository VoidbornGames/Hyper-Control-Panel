import React, { useEffect } from 'react';
import { Alert, AlertTitle, Snackbar } from '@mui/material';
import { useDispatch, useSelector } from 'react-redux';
import { RootState } from '../store';

interface ErrorState {
  message: string;
  severity: 'error' | 'warning' | 'info';
  open: boolean;
}

// This is a simplified version - in a real app, you'd use Redux or Zustand
const ErrorMessage: React.FC = () => {
  const [error, setError] = React.useState<ErrorState>({
    message: '',
    severity: 'error',
    open: false,
  });

  useEffect(() => {
    // Listen for global errors
    const handleError = (event: ErrorEvent) => {
      setError({
        message: event.message || 'An unexpected error occurred',
        severity: 'error',
        open: true,
      });
    };

    // Listen for unhandled promise rejections
    const handleUnhandledRejection = (event: PromiseRejectionEvent) => {
      setError({
        message: event.reason?.message || 'An unexpected error occurred',
        severity: 'error',
        open: true,
      });
    };

    window.addEventListener('error', handleError);
    window.addEventListener('unhandledrejection', handleUnhandledRejection);

    return () => {
      window.removeEventListener('error', handleError);
      window.removeEventListener('unhandledrejection', handleUnhandledRejection);
    };
  }, []);

  const handleClose = () => {
    setError(prev => ({ ...prev, open: false }));
  };

  return (
    <Snackbar
      open={error.open}
      autoHideDuration={6000}
      onClose={handleClose}
      anchorOrigin={{ vertical: 'top', horizontal: 'right' }}
    >
      <Alert
        onClose={handleClose}
        severity={error.severity}
        sx={{ width: '100%' }}
        variant="filled"
      >
        <AlertTitle>Error</AlertTitle>
        {error.message}
      </Alert>
    </Snackbar>
  );
};

// Hook to show errors programmatically
export const useError = () => {
  const [error, setError] = React.useState<ErrorState>({
    message: '',
    severity: 'error',
    open: false,
  });

  const showError = (message: string, severity: 'error' | 'warning' | 'info' = 'error') => {
    setError({
      message,
      severity,
      open: true,
    });
  };

  const clearError = () => {
    setError(prev => ({ ...prev, open: false }));
  };

  return {
    showError,
    clearError,
    error,
  };
};

export default ErrorMessage;