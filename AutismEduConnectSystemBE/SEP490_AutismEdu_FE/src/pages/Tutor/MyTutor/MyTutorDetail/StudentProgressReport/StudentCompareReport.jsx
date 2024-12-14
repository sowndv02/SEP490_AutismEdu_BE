import { Box, Divider, Grid, Modal, Stack, Tab, Tabs, Typography } from '@mui/material'
import React from 'react'
import ArrowDownwardIcon from '@mui/icons-material/ArrowDownward';
import ArrowUpwardIcon from '@mui/icons-material/ArrowUpward';
import { format } from 'date-fns';

const ASC = 1;
const DESC = 2;
const NOT_CHANGE = 3;
function StudentCompareReport({ open, setOpen, selectedItem, compareItem }) {
    const [value, setValue] = React.useState('1');

    const handleChange = (event, newValue) => {
        setValue(newValue);
    };
    const handleClose = () => setOpen(false);

    const getAssessmentPoint = (question, type) => {
        if (!compareItem || !question || !selectedItem) {
            return "";
        }
        let comAss;
        if (type === 1) {
            comAss = compareItem.assessmentResults.find((p) => {
                return p.question === question;
            })
        } else {
            comAss = selectedItem.assessmentResults.find((p) => {
                return p.question === question;
            })
        }
        if (comAss) {
            return comAss.point
        } else {
            return ""
        }
    }
    const getAssessmentChange = (question) => {
        if (!compareItem || !selectedItem || !question) {
            return NOT_CHANGE;
        }
        const compareAss = compareItem.assessmentResults.find((p) => {
            return p.question === question;
        })
        const selectedAss = selectedItem.assessmentResults.find((p) => {
            return p.question === question;
        })
        if (selectedAss && compareAss) {
            if (selectedAss.point > compareAss.point) {
                return DESC;
            } else if (selectedAss.point < compareAss.point) {
                return ASC;
            } else {
                return NOT_CHANGE;
            }
        } else {
            return NOT_CHANGE
        }
    }
    const formatDate = (date) => {
        if (!date) {
            return "";
        }
        const d = new Date(date);
        return `${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`
    }
    return (
        open && <Modal
            open={open}
            onClose={handleClose}
        >
            <Box sx={{
                position: 'absolute',
                top: '50%',
                left: '50%',
                transform: 'translate(-50%, -50%)',
                width: "900px",
                bgcolor: 'background.paper',
                boxShadow: 24,
                p: 4,
                maxHeight: "80vh",
                overflow: 'auto'
            }}>
                <Typography sx={{ textAlign: "center" }} variant='h5'>So sánh 2 sổ liên lạc</Typography>
                <Stack direction='row' mt={5}>
                    <Typography sx={{ width: "50%", textAlign: "center" }}>{format(selectedItem?.from, 'dd/MM/yyyy')} - {format(selectedItem?.to, 'dd/MM/yyyy')}</Typography>
                    <Typography sx={{ width: "50%", textAlign: "center" }}>{format(compareItem?.from, 'dd/MM/yyyy')} - {format(compareItem?.to, 'dd/MM/yyyy')}</Typography>
                </Stack>
                <Box sx={{ width: '100%', mt: 2 }}>
                    <Box sx={{ borderBottom: 1, borderColor: 'divider', display: 'flex', justifyContent: 'center', alignItems: 'center' }}>
                        <Tabs value={value} onChange={handleChange}>
                            <Tab label="Đánh giá" value="1" />
                            <Tab label="Đã làm được" value="2" />
                            <Tab label="Chưa làm được" value="3" />
                            <Tab label="Ghi chú thêm" value="4" />
                        </Tabs>
                    </Box>
                </Box>
                {
                    value === "1" && (
                        <Box px="100px">
                            {
                                selectedItem && (selectedItem.assessmentResults.length >= compareItem.assessmentResults.length) && selectedItem.assessmentResults.map((s) => {
                                    return (
                                        <Grid container mt={2} sx={{ borderBottom: "1px solid gray" }} key={s.id}>
                                            <Grid item xs={3}>
                                                <Stack direction='row' justifyContent='end'>
                                                    <Typography textAlign='right'>{s.point}</Typography>
                                                </Stack>
                                            </Grid>
                                            <Grid item xs={6}>
                                                <Typography sx={{ textAlign: "center", px: 2 }}>{s.question}</Typography>
                                            </Grid>
                                            <Grid item xs={3}>
                                                <Stack direction='row' justifyContent='start' gap={5}>
                                                    <Typography textAlign='left'>{getAssessmentPoint(s.question, 1)}</Typography>
                                                    {
                                                        getAssessmentChange(s.question) === DESC &&
                                                        <ArrowDownwardIcon sx={{ color: "red" }} />
                                                    }
                                                    {
                                                        getAssessmentChange(s.question) === ASC &&
                                                        <ArrowUpwardIcon sx={{ color: "green" }} />
                                                    }
                                                </Stack>
                                            </Grid>
                                        </Grid>
                                    )
                                })
                            }
                            {
                                compareItem && (compareItem.assessmentResults.length > selectedItem.assessmentResults.length) && compareItem.assessmentResults.map((s) => {
                                    return (
                                        <Grid container mt={2} sx={{ borderBottom: "1px solid gray" }} key={s.id}>
                                            <Grid item xs={3}>
                                                <Stack direction='row' justifyContent='end'>
                                                    <Typography textAlign='right'>{getAssessmentPoint(s.question, 2)}</Typography>
                                                </Stack>
                                            </Grid>
                                            <Grid item xs={6}>
                                                <Typography sx={{ textAlign: "center", px: 2 }}>{s.question}</Typography>
                                            </Grid>
                                            <Grid item xs={3}>
                                                <Stack direction='row' justifyContent='start' gap={5}>
                                                    <Typography textAlign='left'>{s.point}</Typography>
                                                    {
                                                        getAssessmentChange(s.question) === DESC &&
                                                        <ArrowDownwardIcon sx={{ color: "red" }} />
                                                    }
                                                    {
                                                        getAssessmentChange(s.question) === ASC &&
                                                        <ArrowUpwardIcon sx={{ color: "green" }} />
                                                    }
                                                </Stack>
                                            </Grid>
                                        </Grid>
                                    )
                                })
                            }
                        </Box>
                    )
                }
                {
                    value === '2' && (
                        <Stack direction='row' justifyContent="center" gap={3} mt={2}>
                            <Box width="50%" px={5}>
                                <Typography sx={{
                                    whiteSpace: "break-spaces", wordBreak: 'break-word',
                                    overflowWrap: 'break-word'
                                }}>{selectedItem?.achieved}</Typography>
                            </Box>
                            <Divider orientation='vertical' flexItem />
                            <Box width="50%" px={5}>
                                <Typography sx={{
                                    whiteSpace: "break-spaces", wordBreak: 'break-word',
                                    overflowWrap: 'break-word'
                                }}>{compareItem?.achieved}</Typography>
                            </Box>
                        </Stack>
                    )
                }
                {
                    value === '3' && (
                        <Stack direction='row' justifyContent="center" gap={3} mt={2}>
                            <Box width="50%" px={5}>
                                <Typography sx={{
                                    whiteSpace: "break-spaces", wordBreak: 'break-word',
                                    overflowWrap: 'break-word'
                                }}>{selectedItem?.failed}</Typography>
                            </Box>
                            <Divider orientation='vertical' flexItem />
                            <Box width="50%" px={5}>
                                <Typography sx={{
                                    whiteSpace: "break-spaces", wordBreak: 'break-word',
                                    overflowWrap: 'break-word'
                                }}>{compareItem?.failed}</Typography>
                            </Box>
                        </Stack>
                    )
                }
                {
                    value === '4' && (
                        <Stack direction='row' justifyContent="center" gap={3} mt={2}>
                            <Box width="50%" px={5}>
                                <Typography sx={{
                                    whiteSpace: "break-spaces", wordBreak: 'break-word',
                                    overflowWrap: 'break-word'
                                }}>{selectedItem?.noteFromTutor || "Không có ghi chú!"}</Typography>
                            </Box>
                            <Divider orientation='vertical' flexItem />
                            <Box width="50%" px={5}>
                                <Typography sx={{
                                    whiteSpace: "break-spaces", wordBreak: 'break-word',
                                    overflowWrap: 'break-word'
                                }}>{compareItem?.noteFromTutor || "Không có ghi chú!"}</Typography>
                            </Box>
                        </Stack>
                    )
                }

            </Box>
        </Modal>
    )
}

export default StudentCompareReport
