import { Box, Divider, Grid, Modal, Stack, Tab, Tabs, Typography } from '@mui/material'
import React from 'react'
import ArrowDownwardIcon from '@mui/icons-material/ArrowDownward';
import ArrowUpwardIcon from '@mui/icons-material/ArrowUpward';
import { format } from 'date-fns';

const ASC = 1;
const DESC = 2;
const NOT_CHANGE = 3;
function CompareReport({ open, setOpen, selectedItem, compareItem }) {
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
    return (
        open && <Modal open={open} onClose={handleClose}>
            <Box
                sx={{
                    position: 'absolute',
                    top: '50%',
                    left: '50%',
                    transform: 'translate(-50%, -50%)',
                    width: "900px",
                    bgcolor: 'background.paper',
                    boxShadow: 24,
                    borderRadius: 2,
                    p: 4,
                    maxHeight: "80vh",
                    overflowY: 'auto'
                }}
            >
                <Typography
                    sx={{
                        textAlign: "center",
                        fontWeight: 'bold',
                        color: 'primary.main',
                        mb: 3
                    }}
                    variant='h5'
                >
                    So sánh 2 sổ liên lạc
                </Typography>
                <Stack direction='row' mt={3}>
                    <Typography
                        sx={{
                            width: "50%",
                            textAlign: "center",
                            fontSize: "16px",
                            fontWeight: 500,
                            color: "text.secondary",
                            borderBottom: "2px solid",
                            borderColor: 'primary.light',
                            pb: 1
                        }}
                    >
                        {format(selectedItem?.from, 'dd/MM/yyyy')} - {format(selectedItem?.to, 'dd/MM/yyyy')}
                    </Typography>
                    <Typography
                        sx={{
                            width: "50%",
                            textAlign: "center",
                            fontSize: "16px",
                            fontWeight: 500,
                            color: "text.secondary",
                            borderBottom: "2px solid",
                            borderColor: 'secondary.light',
                            pb: 1
                        }}
                    >
                        {format(compareItem?.from, 'dd/MM/yyyy')} - {format(compareItem?.to, 'dd/MM/yyyy')}
                    </Typography>
                </Stack>
                <Box sx={{ width: '100%', mt: 4 }}>
                    <Box
                        sx={{
                            borderBottom: 1,
                            borderColor: 'divider',
                            display: 'flex',
                            justifyContent: 'center',
                            alignItems: 'center'
                        }}
                    >
                        <Tabs value={value} onChange={handleChange}>
                            <Tab label="Đánh giá" value="1" sx={{ textTransform: 'none', fontWeight: 'bold' }} />
                            <Tab label="Đã làm được" value="2" sx={{ textTransform: 'none', fontWeight: 'bold' }} />
                            <Tab label="Chưa làm được" value="3" sx={{ textTransform: 'none', fontWeight: 'bold' }} />
                            <Tab label="Ghi chú thêm" value="4" sx={{ textTransform: 'none', fontWeight: 'bold' }} />
                        </Tabs>
                    </Box>
                </Box>

                {value === "1" && (
                    <Box px="100px">
                        {(selectedItem?.assessmentResults || []).map((s, index) => (
                            <Grid
                                container
                                mt={2}
                                sx={{
                                    borderBottom: "1px solid #e0e0e0",
                                    paddingBottom: 2,
                                    alignItems: 'center',
                                    '&:last-of-type': { borderBottom: 'none' }
                                }}
                                key={s.id || index}
                            >
                                <Grid item xs={3}>
                                    <Stack direction='row' justifyContent='end'>
                                        <Typography textAlign='right'>{s.point}</Typography>
                                    </Stack>
                                </Grid>
                                <Grid item xs={6}>
                                    <Typography
                                        sx={{
                                            textAlign: "center",
                                            px: 2,
                                            fontWeight: 500
                                        }}
                                    >
                                        {s.question}
                                    </Typography>
                                </Grid>
                                <Grid item xs={3}>
                                    <Stack direction='row' justifyContent='start' gap={2} alignItems='center'>
                                        <Typography textAlign='left'>{getAssessmentPoint(s.question, 1)}</Typography>
                                        {getAssessmentChange(s.question) === DESC && (
                                            <ArrowDownwardIcon sx={{ color: "red" }} />
                                        )}
                                        {getAssessmentChange(s.question) === ASC && (
                                            <ArrowUpwardIcon sx={{ color: "green" }} />
                                        )}
                                    </Stack>
                                </Grid>
                            </Grid>
                        ))}
                    </Box>
                )}

                {["2", "3", "4"].includes(value) && (
                    <Stack direction='row' justifyContent="center" gap={3} mt={2}>
                        <Box
                            sx={{
                                width: "50%",
                                px: 5,
                                py: 3,
                                bgcolor: 'background.default',
                                borderRadius: 2,
                                boxShadow: "0 2px 8px rgba(0, 0, 0, 0.1)"
                            }}
                        >
                            <Typography
                                sx={{
                                    whiteSpace: "pre-wrap",
                                    wordBreak: 'break-word',
                                    color: "text.primary"
                                }}
                            >
                                {value === "2" && selectedItem?.achieved}
                                {value === "3" && selectedItem?.failed}
                                {value === "4" && (selectedItem?.noteFromTutor || "Không có ghi chú!")}
                            </Typography>
                        </Box>
                        <Divider orientation='vertical' flexItem />
                        <Box
                            sx={{
                                width: "50%",
                                px: 5,
                                py: 3,
                                bgcolor: 'background.default',
                                borderRadius: 2,
                                boxShadow: "0 2px 8px rgba(0, 0, 0, 0.1)"
                            }}
                        >
                            <Typography
                                sx={{
                                    whiteSpace: "pre-wrap",
                                    wordBreak: 'break-word',
                                    color: "text.primary"
                                }}
                            >
                                {value === "2" && compareItem?.achieved}
                                {value === "3" && compareItem?.failed}
                                {value === "4" && (compareItem?.noteFromTutor || "Không có ghi chú!")}
                            </Typography>
                        </Box>
                    </Stack>
                )}
            </Box>
        </Modal>
    )
}

export default CompareReport
