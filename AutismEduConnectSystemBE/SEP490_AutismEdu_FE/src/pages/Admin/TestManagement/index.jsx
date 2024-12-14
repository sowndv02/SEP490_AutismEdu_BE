import { Box, Typography } from '@mui/material'
import React, { useEffect, useState } from 'react'
import TestList from './TestList'

function TestManagement() {
    
    return (
        <Box sx={{
            height: (theme) => `calc(100vh - ${theme.myapp.adminHeaderHeight})`,
            p: 2,
        }}>
            <Box sx={{
                width: "100%", bgcolor: "white", p: "20px",
                borderRadius: "10px",
                boxShadow: "rgba(0, 0, 0, 0.24) 0px 3px 8px"
            }}>
                <TestList />
            </Box>
        </Box>
    )
}

export default TestManagement
