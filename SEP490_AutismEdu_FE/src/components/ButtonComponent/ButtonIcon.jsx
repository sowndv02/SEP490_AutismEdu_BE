import { ArrowForward } from '@mui/icons-material'
import { Button, Typography } from '@mui/material'
import React from 'react'

function ButtonIcon({ width, height, text, action, fontSize }) {
    return (
        <Button
            sx={{
                height: height || "60px",
                width: width || "220px",
                lineHeight: "70px",
                padding: "0 35px",
                color: "white",
                fontSize: fontSize || "14px",
                backgroundImage: 'linear-gradient(to right, #2f57ef, #b966e7, #b966e7, #2f57ef)',
                backgroundSize: "300% 100%",
                backgroundPosition: "0% 50%",
                transition: "background-position 0.3s ease-in-out",
                '&:hover': {
                    backgroundPosition: "100% 50%",
                    '.icon-start': {
                        opacity: 1,
                        transform: "translateX(0)",
                        transitionDelay: "0.225s"
                    },
                    '.icon-end': {
                        opacity: 0,
                        transform: 'translateX(20px)',
                        visibility: 'hidden',
                    },
                    '.btn-text': {
                        transform: "translateX(23px)",
                        transitionDelay: "0.1s"
                    }
                },
            }}
            onClick={action}
        >
            <Typography
                className='btn-text'
                variant="span"
                sx={{
                    transition: "transform 0.6s 0.125s cubic-bezier(0.1, 0.75, 0.25, 1)",
                    marginInlineStart: "-23px",
                }}
            >
                {text || null}
            </Typography>
            <ArrowForward className="icon-start"
                sx={{
                    marginInlineStart: 0,
                    marginInlineEnd: "20px",
                    opacity: 0,
                    transform: "translateX(-10px)",
                    transition: "opacity 0.3s ease, transform 0.3s ease",
                    transitionDelay: "0s",
                    order: -2
                }} />
            <ArrowForward className="icon-end"
                variant="span"
                sx={{
                    marginInlineStart: "20px",
                    transition: "opacity 0.4s 0.25s, transform 0.6s 0.25s",
                    transitionTimingFunction: "cubic-bezier(0.1, 0.75, 0.25, 1)"
                }} />
        </Button>
    )
}

export default ButtonIcon
