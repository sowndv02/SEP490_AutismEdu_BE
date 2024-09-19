import { Button } from '@mui/material';
import React from 'react'

<<<<<<< HEAD
function ButtonComponent({width, height, text, borderRadius, action}) {
    return (
        <Button onClick={action} size='large' sx={{
            width: width,
            height: height,
            color: "white",
            fontWeight: "bold",
=======
function ButtonComponent({ width, height, text, borderRadius, action, fontSize }) {
    return (
        <Button onClick={action} size='large' sx={{
            width: width || "100px",
            height: height || "30px",
            fontSize: fontSize || "14px",
            color: "white",
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
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