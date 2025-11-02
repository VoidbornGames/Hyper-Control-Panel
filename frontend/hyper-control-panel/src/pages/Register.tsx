import React from 'react';
import { Box, Typography, Paper } from '@mui/material';

const Register: React.FC = () => {
  return (
    <Box sx={{ p: 3 }}>
      <Paper sx={{ p: 3, maxWidth: 400, mx: 'auto' }}>
        <Typography variant="h4" gutterBottom>
          Register
        </Typography>
        <Typography>
          Registration form will be implemented here.
        </Typography>
      </Paper>
    </Box>
  );
};

export default Register;