import React from 'react';
import { Box, Typography, Paper } from '@mui/material';

const CreateSite: React.FC = () => {
  return (
    <Box sx={{ p: 3 }}>
      <Paper sx={{ p: 3 }}>
        <Typography variant="h4" gutterBottom>
          CreateSite
        </Typography>
        <Typography>
          CreateSite functionality will be implemented here.
        </Typography>
      </Paper>
    </Box>
  );
};

export default CreateSite;
