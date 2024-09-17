import { ArrowForward } from '@mui/icons-material'
import { Button, Typography } from '@mui/material'
import React from 'react'

function ButtonIcon({ width, height, text, borderRadius, action }) {
    return (
        <Button
            onClick={action}
            sx={{
                width: width,
                height: height,
                borderRadius: borderRadius,
                padding: '10px 20px',
                color: "white",
                backgroundImage: 'linear-gradient(to right, #2f57ef, #b966e7, #b966e7, #2f57ef)',
                backgroundSize: "300% 100%",
                backgroundPosition: "0% 50%",
                transition: "background-position 0.3s ease-in-out",
                '&:hover': {
                    backgroundPosition: "100% 50%",
                    '.icon-start': {
                        opacity: 1,
                        transform: 'translateX(0)',
                        visibility: 'visible',
                        transitionDelay: "0.3s"
                    },
                    '.icon-end': {
                        opacity: 0,
                        transform: 'translateX(20px)',
                        visibility: 'hidden',
                    },
                },
            }}
        >
            <ArrowForward
                className="icon-start"
                sx={{
                    visibility: "hidden",
                    marginRight: '8px',
                    opacity: 0,
                    transition: 'opacity 0.3s ease, transform 0.3s ease',
                    left: '10px',
                    transform: 'translateX(-20px)',
                }}
            />
            <Typography
                variant="body1"
                sx={{
                    margin: '0 8px',
                }}
            >
                {text}
            </Typography>
            <ArrowForward
                className="icon-end"
                sx={{
                    marginLeft: '8px',
                    visibility: "visible",
                    opacity: 1,
                    transition: 'opacity 0.3s ease, transform 0.3s ease',
                    right: '10px',
                }}
            />
        </Button>
    )
}

export default ButtonIcon