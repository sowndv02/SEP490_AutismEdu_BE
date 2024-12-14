import CloseIcon from '@mui/icons-material/Close';
import QuestionMarkIcon from '@mui/icons-material/QuestionMark';
import { Box, IconButton, Modal, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from '@mui/material';
import React from 'react';
function AssessmentDetail({ assessment }) {
    const [open, setOpen] = React.useState(false);
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);
    return (
        <>
            <IconButton sx={{ color: 'red' }} onClick={handleOpen}><QuestionMarkIcon /></IconButton>
            {
                open && assessment && (
                    <Modal
                        open={open}
                    >
                        <Box sx={{
                            position: 'absolute',
                            top: '50%',
                            left: '50%',
                            transform: 'translate(-50%, -50%)',
                            width: 800,
                            bgcolor: 'background.paper',
                            boxShadow: 24,
                            p: 4,
                            height: "70vh"
                        }}>
                            <IconButton
                                onClick={handleClose}
                                sx={{
                                    position: "absolute",
                                    top: 10,
                                    right: 10,
                                    zIndex: 1,
                                    backgroundColor: 'rgba(255, 255, 255, 0.8)'
                                }}
                            >
                                <CloseIcon />
                            </IconButton>
                            <Typography variant='h5'>{assessment.question}</Typography>
                            <Box sx={{ height: "80%", overflow: "auto", mt: 3 }}>
                                <TableContainer component={Paper}>
                                    <Table sx={{ minWidth: 650 }} aria-label="simple table">
                                        <TableHead>
                                            <TableRow>
                                                <TableCell>Điểm</TableCell>
                                                <TableCell>Đánh giá</TableCell>
                                            </TableRow>
                                        </TableHead>
                                        <TableBody>
                                            {assessment.assessmentOptions.map((option) => (
                                                <TableRow
                                                    key={option.id}
                                                >
                                                    <TableCell>
                                                        {option.point}
                                                    </TableCell>
                                                    <TableCell>{option.optionText}</TableCell>
                                                </TableRow>
                                            ))}
                                        </TableBody>
                                    </Table>
                                </TableContainer>
                            </Box>
                        </Box>
                    </Modal>
                )
            }
        </>
    )
}

export default AssessmentDetail
