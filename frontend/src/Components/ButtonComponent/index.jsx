import { Button } from '@mui/material';
import React from 'react'

function ButtonComponent({width, height, text, borderRadius, action}) {
    return (
        <Button onClick={action} size='large' sx={{
            width: width,
            height: height,
            color: "white",
            fontWeight: "bold",
            backgroundImage: 'linear-gradient(to right, #2f57ef, #b966e7, #b966e7, #2f57ef)',
            backgroundSize: "300% 100%",
            backgroundPosition: "0% 50%",
            transition: "background-position 0.3s ease-in-out",
            borderRadius: borderRadius,
            '&:hover': {
                backgroundPosition: "100% 50%",
            }
        }}>
            {text}
        </Button>
    )
}

export default ButtonComponent;