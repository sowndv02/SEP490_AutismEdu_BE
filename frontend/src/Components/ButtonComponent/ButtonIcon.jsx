import { ArrowForward } from '@mui/icons-material'
import { Button, Typography } from '@mui/material'
import React from 'react'

<<<<<<< HEAD
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
=======
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
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
                backgroundImage: 'linear-gradient(to right, #2f57ef, #b966e7, #b966e7, #2f57ef)',
                backgroundSize: "300% 100%",
                backgroundPosition: "0% 50%",
                transition: "background-position 0.3s ease-in-out",
                '&:hover': {
                    backgroundPosition: "100% 50%",
                    '.icon-start': {
                        opacity: 1,
<<<<<<< HEAD
                        transform: 'translateX(0)',
                        visibility: 'visible',
                        transitionDelay: "0.3s"
=======
                        transform: "translateX(0)",
                        transitionDelay: "0.225s"
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
                    },
                    '.icon-end': {
                        opacity: 0,
                        transform: 'translateX(20px)',
                        visibility: 'hidden',
                    },
<<<<<<< HEAD
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
=======
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
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
        </Button>
    )
}

export default ButtonIcon