import { Box, Chip } from '@mui/material'
import React from 'react'

function ChipComponent({ text, bgColor, color }) {
    return (
        <Box sx={{ textAlign: "center", mb: "30px" }}>
            <Chip
                label={text}
                sx={{
                    fontSize: '14px',
                    padding: '20px 10px',
                    bgcolor: bgColor,
                    fontWeight: "bold",
                    color: color
                }}
            />
        </Box>
    )
}

export default ChipComponent
